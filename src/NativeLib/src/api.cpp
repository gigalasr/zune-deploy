#include "api.hpp"
#include "log.h"

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <string>
#include <cstdlib>

#include <ptp/Device.h>
#include <ptp/PipePacketer.h>
#include <usb/DeviceDescriptor.h>
#include <mtpz/TrustedApp.h>
#include <usb/Context.h>
#include <ptp/Device.h>
#include <iostream>
#include <vector>

std::string GetMtpzDataPath() {
	char * home = getenv("HOME");
	return std::string(home? home: ".") + "/.mtpz-data";
}

ZuneDevice::ZuneDevice(mtp::DevicePtr& device, mtp::SessionPtr& session, mtp::TrustedAppPtr& ta)
    : device(device), session(session), ta(ta) {}

ZuneDevice::~ZuneDevice() {}

auto OpenConnection(ZuneDevice::Ptr* out_devicePtr) -> Result {
    *out_devicePtr = 0;

    auto device = mtp::Device::FindFirst();
    if(!device) {
        return Result::ErrorNoDevice;
    }

    auto session = device->OpenSession(1);

    // Expected to fail
    try {
        session->XnaOpenSession();
    } catch (...) {
        std::cout << "Probe Failed (Success)" << std::endl;
    }

    auto ta = mtp::TrustedApp::Create(session, GetMtpzDataPath());

    if(!ta) {
        return Result::ErrorHandshakeFailed;
    }

    ta->Authenticate(true);

    *out_devicePtr = new ZuneDevice(device, session, ta);

    std::cout << "Connected to " << device->GetInfo().Manufacturer << " " << device->GetInfo().Model << " " << device->GetInfo().DeviceVersion << std::endl;

    return Result::Ok;
}

auto CloseConnection(ZuneDevice::Ptr device) -> void {
    // TODO: Close XNA Session and MTP Session cleanly
    delete device;
}

auto PollData(ZuneDevice::Ptr device, std::uint8_t* out_buffer, std::size_t size, std::size_t* out_bytesRead) -> Result {
    auto result = device->session->XnaPollData();
    *out_bytesRead = result.size();

    if(result.empty()) {
         return Result::Ok;
    }

    if(size < result.size()) {
        return Result::ErrorBufferTooSmall;
    }

    std::memcpy(out_buffer, result.data(), std::min(size, result.size()));

    mtp::HexDump("", result, true);

    return Result::Ok;
}

auto SendData(ZuneDevice::Ptr device, std::uint8_t* buffer, std::size_t size) -> void {
    std::vector<std::uint8_t> data(buffer, buffer + size);
    auto response = device->session->XnaSendData(data);
    std::cout << "Response len " << response.size() << std::endl;
}
