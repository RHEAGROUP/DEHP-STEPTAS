////////////////////////////////////////////////////////////////////////////////
// Instructions to complete the solution with STEP-TAS SDK (property of ESA)  //
////////////////////////////////////////////////////////////////////////////////

Prerequesites:
-------------

1) Request and get IITAS_C++_SDK-2.2.4.zip from ESA (This is the C++ code of the STEP-TAS SDK)

2) Software to install on the local computer:
    - Swig  (v4.0.2 recommanded)
    - Ninja
    - Meson
    - CMake
    - MS Visual Studio 2019

  
Convention
----------

DEHP_STEPTAS_REP is the full path to the DEHP-STEPTAS local repository 


STEP 1 : Build the STEP-TAS SDK libraries
-----------------------------------------

- Unzip IITAS_C++_SDK-2.2.4.zip at a location in your computer (=STEPTAS_SDK_PATH)

- Follow instructions in STEP_TAS_SDK_PATH/Readme.txt to build the libraries

  As a result 
     Step.dll, tas_arm.dll and tas_arm_support.dll are generated in STEPTAS_SDK_DLL_PATH
     Step.lib, tas_arm.lib and tas_arm_support.lib are generated in STEPTAS_SDK_LIB_PATH        



STEP 2 : Build steptasint.dll
-----------------------------

- Edit DEHP_STEPTAS_REP\StepTasSDK_Wrapper\StepTasInterface\CMakeLists.txt file
    and change STEPTAS_SDK_PAH accordingly to STEP 1

- Edit DEHP_STEPTAS_REP\StepTasSDK_Wrapper\StepTasInterface\src\CMakeLists.txt
    and change STEPTAS_SDK_LIB_PATH accordingly to STEP 1

- Open a "x64 Native Tools Command Prompt for VS 2019" in DEHP_STEPTAS_REP\StepTasSDK_Wrapper\StepTasInterface\src

- In the command prompt, type: swig.bat 
    As a result of the SWIG call, steptas_wrap.cxx and several .cs files will be generated in the current src directory

- Open a "CMake-GUI"
    - For "Where is the source code" enter : DEHP_STEPTAS_REP/StepTasSDK_Wrapper/StepTasInterface
    - For "Where to build the binaries" enter: DEHP_STEPTAS_REP/StepTasSDK_Wrapper/StepTasInterface/build 
    - Click "Configure" and select : 
		- "Visual Studio 16 2019" as generator
                - "x64" as optional platform for generator
    - Click on "Finish"
    - Click on "Configure" a second time
    - Click on "Generate" to generate the Visual Studio solution

      As a result, a steptasint.sln solution file is created in "Where to build the binaries" directory

- Open steptasint.sln with VS 2019 and build the solution (in "Release" mode)
      
      As a result, steptasint.dll and steptasint.lib are created in DEHP_STEPTAS_REP/StepTasSDK_Wrapper/StepTasInterface/build/src/Release directory 



STEP 3 : Build steptasinterface.dll
-----------------------------------

- In "x64 Native Tools Command Prompt for VS 2019", go to DEHP_STEPTAS_REP/StepTasSDK_Wrapper/StepTasInterface/src directory

- Type : meson setup build_steptasinterface -Dbuildtype=release

    As a result, a build_steptasinterface subdirectory is created with some content

- Type : cd build_steptasinterface

- Type : ninja

    As a result, a steptasinterface.dll file is created in the working directory


STEP 4 : Copy the 5 generated dll files 
---------------------------------------

In this last step, you have to copy the 5 dll files that were generated during STEP 1, STEP 2 and STEP 3.

These files have to be copied in the directory where DEHPSTEPTAS.exe file is generated
by the main solution (DEHP-STEPTAS.sln in DEHP_STEPTAS_REP directory)

- For STEP 1 : Step.dll, tas_arm.dll and tas_arm_support.dll 
- For STEP 2 : steptasint.dll
- For STEP 3 : steptasinterface.dll 

