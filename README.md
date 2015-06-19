# MIC8S-XIC2MIC
Generate MIC8S binary code

# Usage
Example: xic2mic.exe -f test.hex --mh  
  above command will try to generate hex code file for mic8s from PIC14 (default) code  

Options:  
* -f filename   : give source file name, support binary or hex format.  
* -cx src_arch  : specify source architecture:  
                  arc_arch = [PIC14, PIC12, 78X153, 8PX53]  
                  default for PIC14 is src_arch not given.  
* -cm mic_arch  : specify target architecture, here only MIC8S supported.  
                  default for MIC8S is mic_arch not given.   
* --xb          : convert source binary to hex.  
* --mb          : generate binary file for mic8s  
* --mh          : generate hex file for mic8s  
* --ma          : generate abin file for mic8s  
* --fd          : disable byte fill.  
                  default: xic2mic will try to fill ununsed memory region  
* --fz          : specify to fill ununsed region by 0x0, default by 0xff is [--fz] not given.  
* --h           : show this help information.  
