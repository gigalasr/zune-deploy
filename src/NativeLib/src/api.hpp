#include <cstddef>
#include <cstdint>
#include <usb/BulkPipe.h>

#include "enum.hpp"

struct ZuneTransport {
    using Ptr = ZuneTransport*;

    mtp::usb::BulkPipePtr pipe;
};

extern "C" {
auto OpenTransport(std::uint16_t vendorId, std::uint16_t productId, ZuneTransport::Ptr* out_transport) -> Result;
auto CloseTransport(ZuneTransport::Ptr transport) -> void;
auto TransportWrite(ZuneTransport::Ptr transport, const std::uint8_t* buffer, std::size_t size, int timeoutMs) -> Result;
auto TransportRead(ZuneTransport::Ptr transport, std::uint8_t* out_buffer, std::size_t size, std::size_t* out_bytesRead, int timeoutMs) -> Result;
}
