#include "SystemTypes.hpp"

system_uint8 parse_system_uint8(std::string value) {
    return (system_uint8)std::stoul(value, 0, 2);
}

system_int8 parse_system_int8(std::string value) {
    return (system_int8)std::stoi(value, 0, 2);
}

system_bool parse_system_bool(std::string value) {
    return parse_system_uint8(value) == 1;
}

system_uint16 parse_system_uint16(std::string value) {
    return (system_uint16)std::stoul(value, 0, 2);
}

system_int16 parse_system_int16(std::string value) {
    return (system_int16)std::stoi(value, 0, 2);
}

system_uint32 parse_system_uint32(std::string value) {
    return (system_uint32)std::stoul(value, 0, 2);
}

system_int32 parse_system_int32(std::string value) {
    return (system_int32)std::stol(value, 0, 2);
}

system_uint64 parse_system_uint64(std::string value) {
    return (system_uint64)std::stoull(value, 0, 2);
}

system_int64 parse_system_int64(std::string value) {
    return (system_int64)std::stoll(value, 0, 2);
}
