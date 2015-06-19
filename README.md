# MIC8S-XIC2MIC
Generate MIC8S binary code

# Usage
> Example: xic2mic.exe -f test.hex --mh<br>
> > above command will try to generate hex code file for mic8s from PIC14 (default) code

Options:<br>
> -f filename   : give source file name, support binary or hex format.<br>
> -cx src_arch  : specify source architecture:<br>
> > arc_arch = [PIC14, PIC12, 78X153, 8PX53]<br>
> > default for PIC14 is src_arch not given.<br>
> -cm mic_arch  : specify target architecture, here only MIC8S supported.<br>
> > default for MIC8S is mic_arch not given.<br>
> --xb          : convert source binary to hex.<br>
> --mb          : generate binary file for mic8s<br>
> --mh          : generate hex file for mic8s<br>
> --ma          : generate abin file for mic8s<br>
> --fd          : disable byte fill.<br>
> > default: xic2mic will try to fill ununsed memory region<br>
> --fz          : specify to fill ununsed region by 0x0, default by 0xff is [--fz] not given.<br>
> --h           : show this help information.<br>
