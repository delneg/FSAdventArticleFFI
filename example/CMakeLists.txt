cmake_minimum_required(VERSION 3.23)
project(aesxts C)
set(CMAKE_BUILD_TYPE Release)

set(CMAKE_C_STANDARD 11)
set(OPENSSL_USE_STATIC_LIBS TRUE)
find_package(OpenSSL REQUIRED)

set(SOURCE_FILES openssl_aes_xts.c )

#set_target_properties(${PROJECT_NAME} PROPERTIES
#        VERSION ${PROJECT_VERSION}
#        SOVERSION 1
#        PUBLIC_HEADER openssl_aes_xts.h)
add_library(${PROJECT_NAME} SHARED ${SOURCE_FILES})
target_link_libraries(${PROJECT_NAME} OpenSSL::Crypto)