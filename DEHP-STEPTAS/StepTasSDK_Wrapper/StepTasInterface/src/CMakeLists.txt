add_library(steptasint SHARED fileinterface.cxx fileinterface.hxx interface.cxx interface.hxx steptas_wrap.cxx )


# link steptasint with the 3 STEPTAS SDK shared libraries (complete path to step.lib, tas_arm_support.lib and tas_arm.lib)
# replace STEPTAS_SDK_LIB_PATH by the right absolute complete path (3 times)
target_link_libraries(steptasint "STEPTAS_SDK_LIB_PATH/step.lib")
target_link_libraries(steptasint "STEPTAS_SDK_LIB_PATH/tas_arm_support.lib")
target_link_libraries(steptasint "STEPTAS_SDK_LIB_PATH/tas_arm.lib")

