#include "log.h"
#include <chrono>
#include <cstdlib>
#include <iostream>
#include <string>

#include <thread>
#include <usb/DeviceDescriptor.h>
#include <mtpz/TrustedApp.h>
#include <usb/Context.h>
#include <ptp/Device.h>

std::string GetMtpzDataPath() {
	char * home = getenv("HOME");
	return std::string(home? home: ".") + "/.mtpz-data";
}

int main() {
    auto device = mtp::Device::FindFirst();
    if(!device) {
        std::cout << "no zune found" << std::endl;
        return EXIT_FAILURE;
    }

    std::cout << "Device: " << device->GetInfo().GetFilesystemFriendlyName() << std::endl;

    auto session = device->OpenSession(1);
    auto ta = mtp::TrustedApp::Create(session, GetMtpzDataPath());

    if(!ta) {
        std::cout << "Device does not support TrustedApp session. Is device a zune?" << std::endl;
        return EXIT_FAILURE;
    }

    ta->Authenticate(true);

    std::cout << "Opened XNA Session" << std::endl;

    while (true) {
        std::this_thread::sleep_for(std::chrono::milliseconds(500));
        std::cout << "Polling for Data" << std::endl;
        auto data = session->XnaPollData();
        mtp::HexDump("XNA Poll Data", data);
    }

    return EXIT_SUCCESS;
}
