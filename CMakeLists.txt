cmake_minimum_required(VERSION 3.16)
project(zune-deploy)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_CXX_FLAGS -fdiagnostics-color=always)

# Options for andriod-file-transfer-linux
set(BUILD_MTPZ       ON  CACHE BOOL "" FORCE)
set(BUILD_FUSE       OFF CACHE BOOL "" FORCE)
set(BUILD_PYTHON     OFF CACHE BOOL "" FORCE)
set(BUILD_TAGLIB     OFF CACHE BOOL "" FORCE)
set(BUILD_QT_UI      OFF CACHE BOOL "" FORCE)
set(BUILD_SHARED_LIB OFF CACHE BOOL "" FORCE)
add_subdirectory(lib/vendor/android-file-transfer-linux)

add_executable(zune-depoly
    src/main.cpp
)

target_include_directories(zune-depoly PRIVATE lib/vendor/android-file-transfer-linux/mtp)
target_link_libraries(zune-depoly PRIVATE mtp-ng-static)
