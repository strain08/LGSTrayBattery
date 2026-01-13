@echo off
cmake -B build
cmake --build ./build --target hidapi_winapi --config Release