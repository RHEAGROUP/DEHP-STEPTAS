# DEHP STEP-TAS DST

This is the Domain Specific Tool (DST) for the STEP-TAS file format, included as a part of the Thermo-Mechanical Engineering development cluster theme.

It is an **Application Adapter** that enables the user to associate some STEP-TAS entities with *E-TM-10-25* elements through the CDP4.

Microsoft Windows 10 is considered to be the baseline operating systems for these stations. Network access to the hub is required.

## Main features

The DST STEP-TAS Adapter was initially developed in order to demonstrate linking capabilities between an ESATAN model and an engineering model that is shared using COMET Webservices.  

With this software, you will be able to associate/map geometrical entities (with their related computational node id) stored in a STEP-TAS file to some entities (Element Definition / Element Usage) of an engineering model that is shared using COMET Webservices. With this knowledge, the adapter will be able to generate input file for ESATAN that will contain loadings (i.e. dissipation budget) defined on Element Usage / Element Definition on the Hub.  

Once the computation is performed by ESATAN and output files are generated (temperatures at nodes in CSV files), these results can be post-processed (mean, max, min, resampling, …) and uploaded to the hub at the level of associated Element Definition / Element Usage.  

## Building the solution

To build the solution, you need the STEP-TAS SDK that is the property of ESA. That is why this SDK cannot be shared in the Github repository.
You have then first to request the STEP-TAS SDK to ESA, get it and then follow the instructions
that are reported in *DEHP-STEPTAS\StepTasSDK_Wrapper\README.txt* file.
