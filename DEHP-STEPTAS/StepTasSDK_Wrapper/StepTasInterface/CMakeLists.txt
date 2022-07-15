project(steptasint)
cmake_minimum_required(VERSION 3.20)
add_definitions(-DSTEPTASIMPORT)

# the include points to the include directory of STEP-TAS SDK  
# replace STEPTAS_SDK_PATH by the right absolute complete path
include_directories(include "STEPTAS_SDK_PATH/include")

add_subdirectory(src)

