#include <exception>
#include <string>

#ifndef SME_SYSTEM_TYPES_HPP
#define SME_SYSTEM_TYPES_HPP

typedef bool system_bool;

typedef unsigned char system_uint8;
typedef signed char system_int8;

typedef unsigned short system_uint16;
typedef signed short system_int16;

typedef unsigned long system_uint32;
typedef signed long system_int32;

typedef unsigned long long system_uint64;
typedef signed long long system_int64;

system_uint8 parse_system_uint8(std::string value);
system_int8 parse_system_int8(std::string value);
system_bool parse_system_bool(std::string value);
system_uint16 parse_system_uint16(std::string value);
system_int16 parse_system_int16(std::string value);
system_uint32 parse_system_uint32(std::string value);
system_int32 parse_system_int32(std::string value);
system_uint64 parse_system_uint64(std::string value);
system_int64 parse_system_int64(std::string value);

class IProcess {
public:
   virtual void onTick() = 0;
};

class SignalException: public std::exception {
public:
    std::string message;
    SignalException(std::string what) {
        message = what;
    }
    ~SignalException() throw() {}
};

class InvalidReadException: public SignalException {
public:
    InvalidReadException(std::string what)
    : SignalException(what) { }
};
class InvalidDoubleWriteException: public SignalException {
public:
    InvalidDoubleWriteException(std::string what)
    : SignalException(what) { }
};
class IndexOutOfBoundsException : public SignalException {
public:
    IndexOutOfBoundsException(std::string what)
    : SignalException(what) { }
};
class MessageException : public SignalException {
public:
    MessageException(std::string what)
    : SignalException(what) { }
};

#endif /* SME_SYSTEM_TYPES_HPP */
