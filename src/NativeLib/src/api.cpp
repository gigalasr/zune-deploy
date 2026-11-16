#include "api.hpp"
#include "ByteArray.h"

#include <cstddef>
#include <cstdint>
#include <cstring>
#include <memory>

#include <ptp/ByteArrayObjectStream.h>
#include <usb/BulkPipe.h>
#include <usb/Context.h>
#include <usb/Device.h>
#include <usb/DeviceBusyException.h>
#include <usb/DeviceDescriptor.h>

auto OpenTransport(std::uint16_t vendorId, std::uint16_t productId, ZuneTransport::Ptr* out_transport) -> Result {
    *out_transport = nullptr;
    bool foundButNotOpened = false;

    try {
        auto context = std::make_shared<mtp::usb::Context>();
        for (mtp::usb::DeviceDescriptorPtr desc : context->GetDevices()) {
            if (desc->GetVendorId() != vendorId || desc->GetProductId() != productId) {
                continue;
            }

            auto device = desc->TryOpen(context);
            if (!device) {
                foundButNotOpened = true;
                continue;
            }

            auto conf = desc->GetConfiguration(0);
            if (conf->GetInterfaceCount() == 0) {
                // Will it work in mac?
                int index = conf->GetIndex();
                conf.reset();
                device->SetConfiguration(index);
                conf = desc->GetConfiguration(0);
            }

            // We have only a single one
            auto iface = conf->GetInterface(device, conf, 0, 0);
            auto token = device->ClaimInterface(iface);

            *out_transport = new ZuneTransport{mtp::usb::BulkPipe::Create(device, conf, iface, token)};
            return Result::Ok;
        }
    } catch (const mtp::usb::DeviceBusyException&) {
        return Result::ErrorDeviceBusy;
    } catch (...) {
        return Result::ErrorConnectionFailed;
    }

    return foundButNotOpened ? Result::ErrorAccessDenied : Result::ErrorNoDevice;
}

auto CloseTransport(ZuneTransport::Ptr transport) -> void {
    delete transport;
}

auto TransportWrite(ZuneTransport::Ptr transport, const std::uint8_t* buffer, std::size_t size, int timeoutMs) -> Result {
    try {
        transport->pipe->Write(std::make_shared<mtp::ByteArrayObjectInputStream>(mtp::ByteArray(buffer, buffer + size)), timeoutMs);
    } catch (...) {
        return Result::ErrorSendFailed;
    }

    return Result::Ok;
}

auto TransportRead(ZuneTransport::Ptr transport, std::uint8_t* out_buffer, std::size_t size, std::size_t* out_bytesRead, int timeoutMs) -> Result {
    try {
        auto stream = std::make_shared<mtp::ByteArrayObjectOutputStream>();
        transport->pipe->Read(stream, timeoutMs);
        auto& data = stream->GetData();
        if (data.size() > size) {
            return Result::ErrorBufferTooSmall;
        }

        std::memcpy(out_buffer, data.data(), data.size());
        *out_bytesRead = data.size();
    } catch (...) {
        return Result::ErrorReadFailed;
    }

    return Result::Ok;
}
