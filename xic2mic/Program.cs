using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace xic2mic
{
    class Program
    {
        struct CmdInfo
        {
            public string fileName;
            public string chipXName;
            public string chipMName;
            public bool   hexXEnable;
            public bool   binXEnable;
            public bool   binMEnable;
            public bool   hexMEnable;
            public bool   abinMEnable;
            public bool   fillFileEnable;
            public byte   fillCharacter;

            public CmdInfo(string cx, string cm)
            {
                fileName = "";
                chipXName = cx;
                chipMName = cm;
                hexXEnable = false;
                binXEnable = false;
                binMEnable = false;
                hexMEnable = false;
                abinMEnable = false;
                fillFileEnable = true;
                fillCharacter = 0xff;
            }
        };
        
        enum InstLen
        {
	        E_INST_OB = 1,
	        E_INST_TB = 2,
	        E_INST_FB = 4
        };

        struct ChipDev
        {
	        public UInt32  uintBFileLenTop;
            public UInt32  uintCfgBaseAddr;
            public UInt32  uintInstBitOff;		// in bit, 
            //public InstLen instLen;             // in byte
            public byte instLen;
        };

        static CmdInfo cmdInfo = new CmdInfo("PIC14", "MIC8S");
        static ChipDev chipDev = new ChipDev();
        static byte[] s_DataBuf = new byte[16400];
        static uint	   s_DataBufIndex;
        static uint     s_DataBufLength;

        static char[] s_HDataBuf = new char[65600];
        static uint s_HDataBufIndex;
        static uint s_HDataBufLength;
        static bool s_BigEndian = true;

        static void Main(string[] args)
        {
            if (args.Length > 0)  
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--h")
                    {
                        Help();
                        return;
                    }
                    else if (args[i] == "-f")
                    {
                        i++;
                        cmdInfo.fileName = args[i];
                    }
                    else if (args[i] == "-cx")
                    {
                        i++;
                        cmdInfo.chipXName = args[i];
                    }
                    else if (args[i] == "-cm")
                    {
                        i++;
                        cmdInfo.chipMName = args[i];
                    }
                    else if(args[i] == "--xh")
                    {
                        cmdInfo.hexXEnable = true;
                    }
                    else if (args[i] == "--xb")
                    {
                        cmdInfo.binXEnable = true;
                    }
                    else if (args[i] == "--mb")
                    {
                        cmdInfo.binMEnable = true;
                    }
                    else if (args[i] == "--mh")
                    {
                        cmdInfo.hexMEnable = true;
                    }
                    else if (args[i] == "--ma")
                    {
                        cmdInfo.abinMEnable = true;
                    }
                    else if (args[i] == "--fd")
                    {
                        cmdInfo.fillFileEnable = false;
                    }
                    else if (args[i] == "--fz")
                    {
                        cmdInfo.fillCharacter = 0x00;
                    }
                    else if (args[i] == "--ct")
                    {
                        Console.WriteLine(">> xic2mic: filename: {0}", cmdInfo.fileName);
                        Console.WriteLine(">> xic2mic: Xchipname: {0}", cmdInfo.chipXName);
                        Console.WriteLine(">> xic2mic: Mchipname: {0}", cmdInfo.chipMName);
                        Console.WriteLine(">> xic2mic: Xbin: {0}", cmdInfo.binXEnable);
                        Console.WriteLine(">> xic2mic: Mbin: {0}", cmdInfo.binMEnable);
                        Console.WriteLine(">> xic2mic: Mhex: {0}", cmdInfo.hexMEnable);
                        Console.WriteLine(">> xic2mic: Mabin: {0}", cmdInfo.abinMEnable);
                        Console.WriteLine(">> xic2mic: FileFill: {0}", cmdInfo.fillFileEnable);
                        Console.WriteLine(">> xic2mic: FillChar: {0}", cmdInfo.fillCharacter);
                        return;
                    }
                    else
                    {
                        Console.WriteLine(">> xic2mic: unrecognized option '{0}'", args[i]);
                        Console.WriteLine(">> xic2mic: use option '--h' to get more help information");
                        return;
                    }
                }

                //
                if (string.IsNullOrEmpty(cmdInfo.fileName))
                {
                    Console.WriteLine(">> xic2mic: no input file!");
                    Console.WriteLine(">> xic2mic: use option '--h' to get more information about command options");
                    return;
                }

                if (cmdInfo.chipMName.Equals("MIC8S", StringComparison.CurrentCultureIgnoreCase))
                {
                    chipDev.uintBFileLenTop = 0x800;
                    chipDev.uintCfgBaseAddr = 0x400e;
                    chipDev.instLen = 2;// InstLen.E_INST_TB;
                    chipDev.uintInstBitOff = 2;
                }
                ////////////get file fexname ////////////////////
                string fexname = Path.GetExtension(cmdInfo.fileName);

                ////////////// hex to bin //////////////////////////
                if(fexname.Equals(".hex", StringComparison.CurrentCultureIgnoreCase))
                {
                    // conver HEX file to BIN file
                    if (HexFileToBinArray(cmdInfo.fileName) == false)
                    {
                        Console.WriteLine(">> xic2mic: Convert HEX file to Binary Failed!");
                        return;
                    }

                    // store result to bin file;                 
                    if (cmdInfo.binXEnable == true)
                    {
                        string s_BFileName;
                        s_BFileName = cmdInfo.fileName.Substring(0, cmdInfo.fileName.LastIndexOf('.'));
                        s_BFileName = s_BFileName.Insert(s_BFileName.Length, "_xic.bin");
                        
                        if(BinDataBufToFile(s_BFileName, 0, s_DataBufLength) == false)
                        {
                            Console.WriteLine(">> xic2mic: Store Binary file Failed!");
                            return;
                        }
                        Console.WriteLine(">> xic2mic: generate binary file successful!");
                    }
                }
                ///////////hex to bin///////////
                else if (fexname.Equals(".bin", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (BinFileToBinArray(cmdInfo.fileName) == false)
                    {
                        Console.WriteLine(">> xic2mic: Read binary file Failed!");
                        return;
                    }

                    if (cmdInfo.hexXEnable == true)
                    {
                        string s_HFileName = Path.GetFileNameWithoutExtension(cmdInfo.fileName) + "_xic.hex";

                        DataBufBinToHex();

                        if (HexDataBufToFile(s_HFileName, 0, s_HDataBufLength) == false)
                        {
                            Console.WriteLine(">> xic2mic: Store HEX file Failed!");
                            return;
                        }
                        Console.WriteLine(">> xic2mic: generate hex file successful!");
                    }
                }
                else
                {
                    Console.WriteLine(">> xic2mic: Unknown file format!");
                    return;
                }
                // code convert
                if (XBinToMBin() == false)
                {
                    Console.WriteLine(">> xic2mic: code convert failed!");
                    return;
                }
                else
                    Console.WriteLine(">> xic2mic: code convert successful!");

                if (cmdInfo.binMEnable == true)
                {
                    string s_BFileName = Path.GetFileNameWithoutExtension(cmdInfo.fileName) + "_mic.bin";

                    if (BinDataBufToFile(s_BFileName, 0, s_DataBufLength) == false)
                    {
                        Console.WriteLine(">> xic2mic: Store binary file Failed!");
                        return;
                    }
                    Console.WriteLine(">> xic2mic: generate binary file successful!");
                }

                if (cmdInfo.hexMEnable == true)
                {
                    string s_HFileName = Path.GetFileNameWithoutExtension(cmdInfo.fileName) + "_mic.hex";

                    DataBufBinToHex();
                    if (HexDataBufToFile(s_HFileName, 0, s_HDataBufLength) == false)
                    {
                        Console.WriteLine(">> xic2mic: Store Array to HEX file Failed!");
                        return;
                    }
                    Console.WriteLine(">> xic2mic: generate m-hex file successful!");
                }

                //////////bin to abin/////////
                if (cmdInfo.abinMEnable == true)
                {
                    string s_BFileName = Path.GetFileNameWithoutExtension(cmdInfo.fileName) + "_mic.abin";

                    DataBufBinToAbin();

                    if (HexDataBufToFile(s_BFileName, 0, s_HDataBufLength) == false)
                    {
                        Console.WriteLine(">> xic2mic: Store binary file Failed!");
                        return;
                    }
                    Console.WriteLine(">> xic2mic: generate m-abin file successful!");
                }

                Console.WriteLine();
            }  
            else 
            {  
                Console.WriteLine(">> xic2mic: no argument or unknown argument!");
                Help();
            }   
        }

        /******************************************************
         * xic code format covert to mic code format
         *****************************************************/
        static bool XBinToMBin()
        {
            if(cmdInfo.chipXName.Equals("PIC14", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine("xic2mic: Convert from PIC14 to MIC8S...");
                return (Pic14BinToMBin());
            }
            else if (cmdInfo.chipXName.Equals("PIC12", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine(">> xic2mic: Convert from PIC12 to MIC8S...");
                return (Pic12BinToMBin());
            }
            else if(cmdInfo.chipXName.Equals("78X153", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine(">> xic2mic: Convert from 78X153 to MIC8S...");
                return (EicBinToMBin());
            }
            else if(cmdInfo.chipXName.Equals("8PX53", StringComparison.CurrentCultureIgnoreCase))
            {
                Console.WriteLine(">> xic2mic: Convert from 8PX53 to MIC8S...");
                return (FicBinToMBin());
            }
            
            Console.WriteLine(">> xic2mic: Unknown core name of XIC!");
            return false;
        }

        static bool Pic14BinToMBin()
        {
	        uint   filelength;
	        byte[]  bytebuf = new byte[4];
	        uint   fileposition;
            int unit = (int)chipDev.instLen;
	
	        filelength = s_DataBufLength;
	        s_DataBufIndex = 0;

	        filelength = (filelength + 1) & 0xfffffffe;
	        if(filelength > chipDev.uintBFileLenTop)
                filelength = chipDev.uintBFileLenTop;

	        
		    while(filelength > 0)
		    {
			    fileposition = s_DataBufIndex;
			    ReadDataBuf(bytebuf, unit);
                Pic14ToMIC8S_unit(bytebuf);
			    s_DataBufIndex = fileposition;
			    WriteDataBuf(bytebuf, 0, unit);
			    filelength = (uint)(filelength - unit);
		    }
	     

	        if(s_DataBufLength >= 0x400e)
	        {
		        s_DataBufIndex = 0x400e;
		        ReadDataBuf(bytebuf, unit);
		        bytebuf[2] = bytebuf[1];
		        bytebuf[3] = 0xff;
		        bytebuf[1] = 0xff;
		        s_DataBufIndex = chipDev.uintBFileLenTop;
		        WriteDataBuf(bytebuf, 0, unit);
		        WriteDataBuf(bytebuf, 2, unit);
		        s_DataBufLength = s_DataBufIndex;
	        }
	        else
	        {
		        if(s_DataBufLength > chipDev.uintBFileLenTop)
			        s_DataBufLength = chipDev.uintBFileLenTop;
	        }

            return true;
        }
        
        static void Pic14ToMIC8S_unit(byte[] pBuf)
        {
	        int code;
	        int codeCategory;
	        int inst;
	        int bitshift;
            code = pBuf[0];
            code += pBuf[1] << 8;
		
	        if((code & 0xc000) != 0)
		        return;
	        code = code & 0x3fff;

	        //codeCategory = code & g_categorymask;
	        codeCategory = code >> 12;
	
	        inst = code & 0xfff;

	
	        switch(codeCategory)
	        {
	        case 0:
		        bitshift = 8;
		        switch(inst >> bitshift)
		        {
		        case 0: // movwf nop clrwdt retfie return  sleep
			        if(((inst >> (bitshift - 1)) & 0x1) == 1) // movwf
				        code = inst;
			        else
				        code = inst & 0xf;
			        break;
		        case 1:
			        if(((inst >> (bitshift - 1)) & 1) != 1) // CLRF
				        code = 1;
			        //else							// CLRW
				    //    code = code;
			        break;
		        default:
			        break;
		        }
		        break;
	        case 1:
		        break;
	        case 2:
		        bitshift = 11;
		        switch(inst >> bitshift)
		        {
		        case 0: // call
			        code = code | (1 << (bitshift+1));
			        break;
		        case 1:	// goto 
			        code = code & (~(1 << bitshift));
			        break;
		        }
		        break;
	        case 3:
		        bitshift = 9;
		        switch(inst >> bitshift)
		        {
		        case 0:// movelw
			        code = code & (~(0xf << 8));
			        code |= (0xd << 8);
			        break;
		        case 1:// movelw
			        code = code & (~(0xf << 8));
			        code |= (0xd << 8);
			        break;
		        case 2:
			        code = code & (~(0xf << 8));
			        code |= (0xc << 8);
			        break;
		        case 3:
			        code = code & (~(0xf << 8));
			        code |= (0xc << 8);
			        break;
		        case 4:
			        if(((inst >> (bitshift - 1))  & 1) == 0x1) // andlw
			        {
				        code = code & 0xff;
				        code += 0x2c00;
			        }
			        else //iorlw
			        {
				        code = code & 0xff;
				        code += 0x3800;
			        }
			        break;
		        case 5:
			        code = code & 0xff;
			        code += 0x2d00;
			        break;
		        case 6:
			        code = code & 0xff;
			        code += 0x2800;
			        break;
		        case 7:
			        code = code & 0xff;
			        code += 0x2900;
			        break;
		        }
		        break;
	        }
	
	        pBuf[0] = (byte)code;
	        code = code >> 8;
            pBuf[1] = (byte)code;
	
        }

        static bool Pic12BinToMBin()
        {
            uint filelength;
            byte[] bytebuf = new byte[4];
            uint fileposition;
            int unit = (int)chipDev.instLen;

            filelength = s_DataBufLength;
            s_DataBufIndex = 0;

            filelength = filelength & 0xfffffffe;
            if (filelength > chipDev.uintBFileLenTop)
                filelength = chipDev.uintBFileLenTop;

            while (filelength > 0)
            {
                fileposition = s_DataBufIndex;
                ReadDataBuf(bytebuf, unit);
                Pic12ToMIC8S_unit(bytebuf);
                s_DataBufIndex = fileposition;
                WriteDataBuf(bytebuf, 0, unit);
                filelength = (uint)(filelength - unit);
            }

            return true;
        }

        static void Pic12ToMIC8S_unit(byte[] pBuf)
        {
            int code;
            int bitfield;

            code = pBuf[0];
            code += pBuf[1] << 8;

            if ((code & 0xf000) != 0)
                return; 

            code = code & 0xfff;

            
            bitfield = (code & 0xf00) >> 8;//[11-8]
            switch (bitfield)
            {
                case 0:
                    bitfield = (code & 0xf0) >> 4;//[7-4]
                    switch (bitfield)
                    {
                        case 0:
                            bitfield = code & 0xf;//[3-0]
                            switch (bitfield)
                            {
                                case 0:// nop
                                    code = 0;
                                    break;
                                case 1:// 
                                    break;
                                case 2: // a->cont(MOVWF) OPTION
                                    code = 0x81;
                                    break;
                                case 3:	// sleep
                                    code = 0x3;
                                    break;
                                case 4: // wdtc(clrwdt)
                                    code = 4;
                                    break;
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                    code = bitfield;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 1:// empty
                            break;
                        case 2:// 
                        case 3:// MOVWF
                            code = 0x80 | (code & 0x1f);
                            break;
                        case 4:// CLRW
                            code = 0x1;
                            break;
                        case 5: //empty
                            break;
                        case 6:
                        case 7:// CLRF
                            code = 0x180 | (code & 0x1f);
                            break;
                        case 8://
                        case 9:// 
                        case 10:// 
                        case 11:// SUBWF
                            code = 0x200 | ((code & 0x20) << 2) | (code & 0x1f);
                            break;
                        case 12:
                        case 13:
                        case 14:
                        case 15: // DECF
                            code = 0x300 | ((code & 0x20) << 2) | (code & 0x1f);
                            break;
                    }
                    break;
                /*case 1://
                    if ((code & 0xc0) == 0x0) // IORWF
                        code = 0x400 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x40)  //ANDWF
                        code = 0x500 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x80)  //XORWF
                        code = 0x600 | ((code & 0x20) << 2) | (code & 0x1f);
                    else                            //ADDWF
                        code = 0x700 | ((code & 0x20) << 2) | (code & 0x1f);
                    break;
                case 2:
                    if ((code & 0xc0) == 0x0) // MOVF
                        code = 0x800 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x40)  //COMF
                        code = 0x900 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x80)  //INCF
                        code = 0xa00 | ((code & 0x20) << 2) | (code & 0x1f);
                    else                            //DECFSZ
                        code = 0xb00 | ((code & 0x20) << 2) | (code & 0x1f);
                    break;
                case 3://
                    if ((code & 0xc0) == 0x0) // RRF
                        code = 0xc00 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x40)  //RLF
                        code = 0xd00 | ((code & 0x20) << 2) | (code & 0x1f);
                    else if ((code & 0xc0) == 0x80)  //SWAPF
                        code = 0xe00 | ((code & 0x20) << 2) | (code & 0x1f);
                    else                            //INCFSZ
                        code = 0xf00 | ((code & 0x20) << 2) | (code & 0x1f);
                    break;*/
                case 1:// IORWF ANDWF  XORWF ADDWF
                case 2:// MOVF COMF INCF DECFSZ
                case 3:// RRF  RLF SWAPF INCFSZ
                    code = ((code & 0x3e0) << 2) | (code & 0x1f);
                    break;
                case 4:// BCF
                case 5:// BSF
                case 6://BTFSC
                case 7://BTFSS
                    code = ((code & 0x7e0) << 2) | (code & 0x1f);
                    break;
                case 8://RETLW
                    code = 0x3c00 | (code & 0xff);
                    break;
                case 9://CALL
                    code = 0x3000 | (code & 0xff);
                    break;
                case 10://GOTO
                case 11:// GOTO
                    code = 0x2000 | (code & 0x1ff);
                    break;
                case 12://MOVLW
                    code = 0x3d00 | (code & 0xff);
                    break;
                case 13://IORLW
                    code = 0x3800 | (code & 0xff);
                    break;
                case 14://ANDLW
                    code = 0x2c00 | (code & 0xff);
                    break;
                case 15:// XORLW
                    code = 0x2d00 | (code & 0xff);
                    break;
            }
            pBuf[0] = (byte)code;
            code = code >> 8;
            pBuf[1] = (byte)code;
        }

        static bool EicBinToMBin()
        {
	        uint   filelength;
	        byte[]  bytebuf = new byte[4];
	        uint   fileposition;
            int unit = (int)chipDev.instLen;

	        filelength = s_DataBufLength;
	        s_DataBufIndex = 0;

            filelength = filelength & 0xfffffffe;
            if (filelength > chipDev.uintBFileLenTop)
                filelength = chipDev.uintBFileLenTop;
            	       
		    while(filelength > 0)
		    {
			    fileposition = s_DataBufIndex;
			    ReadDataBuf(bytebuf, unit);
                EicToMIC8S_unit(bytebuf);
			    s_DataBufIndex = fileposition;
			    WriteDataBuf(bytebuf, 0, unit);
			    filelength = (uint)(filelength - unit);
		    }
	       
            return true;
        }

        static void EicToMIC8S_unit(byte[] pBuf)
        {
	        int code;
	        int bitfield;

            code = pBuf[0];
            code += pBuf[1] << 8;

            if ((code & 0xe000) != 0)
                return;

	        code = code & 0x1fff;

	        if((code & 0x1000) == 0x1000)	// call jmp mov or and xor retl sub int add
	        {
		        bitfield = (code & 0xf00) >> 8;
		        switch(bitfield)
		        {
		        case 0:
		        case 1:
		        case 2:
		        case 3:			//call
			        code = 0x3000 | (code & 0x3ff);
			        break;
		        case 4:
		        case 5:
		        case 6:
		        case 7:			//jmp(goto)
			        code = 0x2000 | (code & 0x3ff);
			        break;
		        case 8:			// mov(movlw)
			        code = 0x3d00 | (code & 0xff);
			        break;
		        case 9:			// or(iorlw)
			        code = 0x3800 | (code & 0xff);
			        break;
		        case 10:		// and(andlw)
			        code = 0x2c00 | (code & 0xff);
			        break;
		        case 11:		// xor(xorlw)
			        code = 0x2d00 | (code & 0xff);
			        break;
		        case 12:		// retl
			        code = 0x3c00 | (code & 0xff);
			        break;
		        case 13:		// sub(sublw)
			        code = 0x2800 | (code & 0xff);
			        break;
		        case 14:		// int
			        code = 0xf;
			        break;
		        case 15:		// add
			        code = 0x2900 | (code & 0xff);
			        break;
		        }
	        }
	        else
	        {
		        bitfield = (code & 0xf00) >> 8;//[11-8]
		        switch(bitfield)
		        {
		        case 0:
			        bitfield = (code & 0xf0) >> 4;//[7-4]
			        switch(bitfield)
			        {
			        case 0:
				        bitfield = code & 0xf;//[3-0]
				        switch(bitfield)
				        {
				        case 0:// nop
					        code = 0;
					        break;
				        case 1:// daa
					        code = 0xa;
					        break;
				        case 2: // a->cont(MOVWF)
					        code = 0x81;
					        break;
				        case 3:	// sleep
				        case 4: // wdtc(clrwdt)
					        code = bitfield;
					        break;
				        default: // iow(a->iocr)(MOVF)
					        code = 0x80 | (code & 0xf);
                            break;
				        }
				        break;
			        case 1:
				        bitfield = code & 0xf;//[3-0]
				        switch(bitfield)
				        {
				        case 0:// eni bsf
					        code = 0x1701;
					        break;
				        case 1:// disi bcf
					        code = 0x1301;
					        break;
				        case 2:// ret
					        code = 4;
					        break;
				        case 3:// reti
					        code = 5;
					        break;
				        case 4: /// CONTR movf
					        code = 0x801;
					        break;
				        default: // iocr->a(MOVF 
					        code = 0x800 | (code & 0xf);
                            break;
				        }
				        break;
			        case 2:// empty
			        case 3:// empty
				        break;
			        case 4:
			        case 5:
			        case 6:
			        case 7:// MOVWF
				        code = 0x80 | (code & 0x3f);
				        break;
			        case 8://CLRA(CLRW)
				        code = 0x1;
				        break;
			        case 9:// empty
			        case 10:// empty
			        case 11:// empty
				        break;
			        case 12:
			        case 13:
			        case 14:
			        case 15: // CLR R
				        code = 0x180 | (code & 0x3f);
                        break;
			        }
			        break;
		        case 1:
		        case 2:
		        case 3:
		        case 4:
		        case 5:
		        case 6:
		        case 7:
			        code = (code & 0x3f) | ((code & 0xfc0) << 1);
			        break;
		        case 8:
		        case 9:	// BC RB(RCF)
			        code = ((code & 0x1c0) << 1) | (code & 0x3f) | 0x1000;
			        break;
		        case 10:
		        case 11:// BS (BSF)
			        code = ((code & 0x1c0) << 1) | (code & 0x3f) | 0x1400;
			        break;
		        case 12:
		        case 13:// jbc(BTFSC )
			        code = ((code & 0x1c0) << 1) | (code & 0x3f) | 0x1800;
			        break;
		        case 14:
		        case 15:// jbs(BTFss
			        code = ((code & 0x1c0) << 1) | (code & 0x3f) | 0x1c00;
			        break;
		        }
	        }

            pBuf[0] = (byte)code;
	        code = code >> 8;
            pBuf[1] = (byte)code;
        }

        static bool FicBinToMBin()
        {
            uint filelength;
            byte[] bytebuf = new byte[4];
            uint fileposition;
            int unit = (int)chipDev.instLen;

            filelength = s_DataBufLength;
            s_DataBufIndex = 0;

            filelength = filelength & 0xfffffffe;
            if (filelength > chipDev.uintBFileLenTop)
                filelength = chipDev.uintBFileLenTop;

            while (filelength > 0)
            {
                fileposition = s_DataBufIndex;
                ReadDataBuf(bytebuf, unit);
                FicToMIC8S_unit(bytebuf);
                s_DataBufIndex = fileposition;
                WriteDataBuf(bytebuf, 0, unit);
                filelength = (uint)(filelength - unit);
            }

            return true;
        }

        static void FicToMIC8S_unit(byte[] pBuf)
        {
            int code;
            int bitfield;

            code = pBuf[0];
            code += pBuf[1] << 8;

            if ((code & 0xe000) != 0)
                return;

            code = code & 0x1fff;

            if ((code & 0x1000) == 0x1000)	// BTFSC BTFSS BSF BCF ADCWF SBCWF MOVLW XORLW IORLW ADDLW ANDLW RETLW SUBLW
            {
                bitfield = (code & 0xf00) >> 8;
                switch (bitfield)
                {
                    case 0:
                    case 1:         // btfss
                        code = 0x1c00 | ((code & 0x1c0) << 1) | (code & 0x3f);
                        break;
                    case 2:
                    case 3:			// btfsc
                        code = 0x1800 | ((code & 0x1c0) << 1) | (code & 0x3f);
                        break;
                    case 4:
                    case 5:         // BSF
                        code = 0x1400 | ((code & 0x1c0) << 1) | (code & 0x3f);
                        break;     
                    case 6:
                    case 7:			// BCF
                        code = 0x1000 | ((code & 0x1c0) << 1) | (code & 0x3f);
                        break;
                    case 8:			// ADCWF, SDCWF
                        if ((code & 0x800) == 0)
                            code = 0x3a00 | ((code & 0x40) << 1) | (code & 0x3f);  // ADCWF
                        else
                            code = 0x3b00 | ((code & 0x40) << 1) | (code & 0x3f);  // SDCWF
                        break;
                    case 9:			// MOVLW
                        code = 0x3d00 | (code & 0xff);
                        break;
                    case 10:		// XORLW
                        code = 0x2d00 | (code & 0xff);
                        break;
                    case 11:		//IORLW
                        code = 0x3800 | (code & 0xff);
                        break;
                    case 12:		// ADDLW
                        code = 0x2900 | (code & 0xff);
                        break;
                    case 13:		// ANDLW
                        code = 0x2c00 | (code & 0xff);
                        break;
                    case 14:		// RETLW
                        code = 0x3c00 | (code & 0xff);
                        break;
                    case 15:		// SUBLW
                        code = 0x2800 | (code & 0xff);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                bitfield = (code & 0xf00) >> 8;//[11-8]
                switch (bitfield)
                {
                    case 0:
                        bitfield = (code & 0xf0) >> 4;//[7-4]
                        switch (bitfield)
                        {
                            case 0:
                                bitfield = code & 0xf;//[3-0]
                                switch (bitfield)
                                {
                                    case 0:// nop
                                        code = 0;
                                        break;
                                    case 1:// CLRWDT
                                        code = 0x4;
                                        break;
                                    case 2: // a->cont(MOVWF)
                                        code = 0x81;
                                        break;
                                    case 3:	// sleep
                                        code = 0x3;
                                        break;
                                    case 4: // wdtc(clrwdt)
                                        code = bitfield;
                                        break;
                                    default: // iow(a->iocr)(MOVF)
                                        code = 0x80 | (code & 0xf);
                                        break;
                                }
                                break;
                            case 1:// empty
                            case 2:// empty
                                break;
                            case 3:// empty
                                bitfield = code & 0xf;//[3-0]
                                switch (bitfield)
                                {
                                    case 10:// INT
                                        code = 0xf;
                                        break;
                                    case 12:// DAA
                                        code = 0xa;
                                        break;
                                    case 13:// DAS
                                        code = 0xb;
                                        break;
                                    case 14:// RETURN
                                        code = 0x4;
                                        break;
                                    case 15: /// RETFIE
                                        code = 0x5;
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case 4:
                            case 5:
                            case 6:
                            case 7:// MOVWF
                                code = 0x80 | (code & 0x3f);
                                break;
                            case 8://CLRA(CLRW)
                                code = 0x1;
                                break;
                            case 9:// empty
                            case 10:// empty
                            case 11:// empty
                                break;
                            case 12:
                            case 13:
                            case 14:
                            case 15: // CLR R
                                code = 0x180 | (code & 0x3f);
                                break;
                        }
                        break;
                    case 1://
                        if ((code & 0x80) == 0x0) // COMF
                            code = 0x900 | ((code & 0x40) << 1) | (code & 0x3f);
                        else //MOVF
                            code = 0x800 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 2:
                        if ((code & 0x80) == 0x0) // ANDWF
                            code = 0x500 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // ADDWF
                            code = 0x700 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 3://
                        if ((code & 0x80) == 0x0) // DECF
                            code = 0x300 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // DECFSZ
                            code = 0xb00 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 4:// 
                        if ((code & 0x80) == 0x0) // INCF
                            code = 0xa00 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // INCFSZ
                            code = 0xf00 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 5:// 
                        if ((code & 0x80) == 0x0) // SUBWF
                            code = 0x200 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // XORWF
                            code = 0x600 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 6:
                        if ((code & 0x80) == 0x0) // RLF
                            code = 0xd00 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // SWAPF
                            code = 0xe00 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 7:
                        if ((code & 0x80) == 0x0) // RRF
                            code = 0xc00 | ((code & 0x40) << 1) | (code & 0x3f);
                        else // IORWF
                            code = 0x400 | ((code & 0x40) << 1) | (code & 0x3f);
                        break;
                    case 8:
                    case 9:	
                    case 10:
                    case 11:// CALL
                        code = 0x3000 | (code & 0x3ff);
                        break;
                    case 12:
                    case 13:
                    case 14:
                    case 15:// GOTO
                        code = 0x2000 | (code & 0x3ff);
                        break;
                }
            }

            pBuf[0] = (byte)code;
            code = code >> 8;
            pBuf[1] = (byte)code;
        }
        
        static void ReadDataBuf(byte[] pbuf, int len)
        {
	        int i;
	        for(i = 0; i < len; i++)
		        pbuf[i] = s_DataBuf[s_DataBufIndex++];
        }

        static void WriteDataBuf(byte[] pbuf, int start, int len)
        {
            int i;
            for (i = 0; i < len; i++)
                s_DataBuf[s_DataBufIndex++] = pbuf[start + i];
        }
        static void WriteHDataBuf(char[] pbuf, int start, int len)
        {
            int i;
            for (i = 0; i < len; i++)
                s_HDataBuf[s_HDataBufIndex++] = pbuf[start + i];
        }

        ///////////////////////////////////////////////////////
        ///////////		Data Buffer to BIN file	///////////////
        ///////////////////////////////////////////////////////
        static bool BinDataBufToFile(string DFileName, uint startaddr, uint DataLen)
        {
	        FileStream dfile;

            try
            {
                dfile = new FileStream(DFileName, FileMode.OpenOrCreate);
            }
            catch
            {
                Console.WriteLine(">> xic2mic: Can not Create or Open file!");
                return false;
            }
            dfile.Position = 0;
            BinaryWriter wdfile = new BinaryWriter(dfile);
            wdfile.Write(s_DataBuf, (int)startaddr, (int)DataLen);
            dfile.SetLength(dfile.Position);
            wdfile.Close();
	        dfile.Close();
            return true;
        }

         ///////////////////////////////////////////////////////
        ///////////		Data Buffer to HEX file ///////////////
        ///////////////////////////////////////////////////////
        static bool HexDataBufToFile(string DFileName, uint startaddr, uint DataLen)
        {
            FileStream dfile;
           
            try
            {
                dfile = new FileStream(DFileName, FileMode.OpenOrCreate);
            }
            catch
            {
                Console.WriteLine(">> xic2mic: Can not Create or Open file!");
                return false;
            }
            /* open destination file */

            dfile.Position = 0;
            BinaryWriter wdfile = new BinaryWriter(dfile);
            wdfile.Write(s_HDataBuf, (int)startaddr, (int)DataLen);
            dfile.SetLength(dfile.Position);
            wdfile.Close();
            dfile.Close();

            return true;
        }

         ///////////////////////////////////////////////////////
        ///////////		Data Buffer BIN to HEX   ///////////////
        ///////////////////////////////////////////////////////
        static bool DataBufBinToHex()
        {
	        char sum;
	        uint filelength;
	        byte linelen;
            int  i, j;
            UInt16 startaddr;

            byte[] byteTmp = new byte[16];
            char[] charTmp = new char[32];
	
	        filelength = s_DataBufLength;

	        s_DataBufIndex = 0;
            s_HDataBufIndex = 0;
            startaddr = 0;

	        while(filelength > 0)
	        {
                s_HDataBuf[s_HDataBufIndex++] = ':';

		        if(filelength > 16)
			        linelen = 16;
		        else
                    linelen = (byte)filelength;
                byteTmp[0] = linelen;
                j = ByteToChar(byteTmp, charTmp, 1);
                for(i = 0; i < j; i++)
                    s_HDataBuf[s_HDataBufIndex++] = charTmp[i];
		        
		        byteTmp[0] = (byte)((startaddr & 0xff00) >> 8);
                j = ByteToChar(byteTmp, charTmp, 1);
                for(i = 0; i < j; i++)
                    s_HDataBuf[s_HDataBufIndex++] = charTmp[i];

		        byteTmp[0] = (byte)(startaddr & 0xff);
                j = ByteToChar(byteTmp, charTmp, 1);
                for(i = 0; i < j; i++)
                    s_HDataBuf[s_HDataBufIndex++] = charTmp[i];

		        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
		        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;

                for(i = 0; i < linelen; i++)
                    byteTmp[i] = s_DataBuf[s_DataBufIndex++];
		        j = ByteToChar(byteTmp, charTmp, linelen);
                for(i = 0; i < j; i++)
                    s_HDataBuf[s_HDataBufIndex++] = charTmp[i];

		        filelength -= linelen;
                sum = CheckSum(byteTmp, (char)linelen, linelen);
                byteTmp[0] = (byte)(startaddr & 0xff);
                byteTmp[1] = (byte)((startaddr >> 8) & 0xff);
                sum = (char)(-CheckSum(byteTmp, sum, 2));
                	       
		        startaddr += linelen;

                byteTmp[0] = (byte)sum;
                j = ByteToChar(byteTmp, charTmp, 1);
                for(i = 0; i < j; i++)
                    s_HDataBuf[s_HDataBufIndex++] = charTmp[i];
               
		        s_HDataBuf[s_HDataBufIndex++] = (char)0x0d;
		        s_HDataBuf[s_HDataBufIndex++] = (char)0x0a;
	        }
	        s_HDataBuf[s_HDataBufIndex++] = ':';
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x30;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x31;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x46;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x46;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x0d;
	        s_HDataBuf[s_HDataBufIndex++] = (char)0x0a;
            s_HDataBufLength = s_HDataBufIndex;
            return true;
        }

        /******************************************************
         * bin array to abin array
         *****************************************************/
        static bool DataBufBinToAbin()
        {
            int i;
	        byte perlength;
	        char[][]   AsciiBuf = new char[4][];
            AsciiBuf[0] = new char[8];
            AsciiBuf[1] = new char[8];
            AsciiBuf[2] = new char[8];
            AsciiBuf[3] = new char[8];

	        //CFile dfile;
	        byte[] pbuf = new byte[2];
	        uint filelength; 
	        byte Colmn;
	        int  bitstart = (int)chipDev.uintInstBitOff;
	        byte j;
	
	        
	        if(cmdInfo.fillFileEnable == true)
	        {
		        if(s_DataBufLength == chipDev.uintBFileLenTop)
		        {
			        pbuf[0] = 0xff;
			        s_DataBufIndex = s_DataBufLength;
			        for(j = 0; j < 8; j++)
				        WriteDataBuf(pbuf, 0, 1);
		        }
                else if (s_DataBufLength <= chipDev.uintBFileLenTop + 4)
		        {
			        pbuf[0] = 0xff;
			        s_DataBufIndex = s_DataBufLength;
                    for (j = 0; j < (chipDev.uintBFileLenTop + 8 - s_DataBufLength); j++)
                        WriteDataBuf(pbuf, 0, 1);

			        pbuf[0] = 0x5a;
                    s_DataBufIndex = chipDev.uintBFileLenTop + 6;
                    WriteDataBuf(pbuf, 0, 1);
		        }
		        else
                {
                    pbuf[0] = 0xff;
                    s_DataBufIndex = chipDev.uintBFileLenTop + 4;
                    for (j = 0; j < (chipDev.uintBFileLenTop + 8 - s_DataBufLength); j++)
                        WriteDataBuf(pbuf, 0, 1);
		        }
                s_DataBufLength = chipDev.uintBFileLenTop + 8;
	        }

	        /* the length of the source file */
	        filelength = s_DataBufLength;

	        bool bBigEndian;

	        bBigEndian = s_BigEndian;
            perlength = chipDev.instLen;

	        s_DataBufIndex = 0;
            s_HDataBufIndex = 0;
	        if(bBigEndian == false)
	        {
		        i=0;
		        Colmn = perlength;
		        while(s_DataBufIndex != filelength)
		        {
			        ReadDataBuf(pbuf,1);
			        BinaryToAscii(pbuf[0], AsciiBuf[0]);	
			        WriteHDataBuf(AsciiBuf[0],0, 8);
			        if(s_DataBufIndex == filelength)
			        {
				        break;	
			        }
			        i++;
			        if((i%Colmn)== 0)
			        {		
			        }		
		        }

		        i = i % Colmn;
		        // file tail
		        if(i != 0)
		        {
			        pbuf[0] = 0xff;
			        BinaryToAscii(pbuf[0], AsciiBuf[0]);		
			        for(; i < Colmn; i++)
                    {
                        WriteHDataBuf(AsciiBuf[0], 0, 8);
			        }
                    AsciiBuf[0][0] = '\n';
                    WriteHDataBuf(AsciiBuf[0], 0, 1);
		        }
	        }
	        else
	        {
		        i=0;
		        Colmn = perlength;
		        while(s_DataBufIndex != filelength)
		        {
			        ReadDataBuf(pbuf,1);
			        BinaryToAscii(pbuf[0],AsciiBuf[i%perlength]);	
			        i++;
			        if((i%Colmn)== 0)
			        {
				        switch(Colmn)
				        {
					        case 4:
                                WriteHDataBuf(AsciiBuf[3], bitstart, 8 - bitstart);
                                WriteHDataBuf(AsciiBuf[2], 0, 8);
                                WriteHDataBuf(AsciiBuf[1], 0, 8);
                                WriteHDataBuf(AsciiBuf[0], 0, 8);
						        break;
					        case 2:
                                WriteHDataBuf(AsciiBuf[1], bitstart, 8 - bitstart);
                                WriteHDataBuf(AsciiBuf[0], 0, 8);
						        break;
					        case 1:
                                WriteHDataBuf(AsciiBuf[0], 0, 8);
						        break;
					        default:
						        break;
				        }
				        if(s_DataBufIndex == filelength)
				        {
					        break;
                        }
                        AsciiBuf[0][0] = '\n';
                        WriteHDataBuf(AsciiBuf[0], 0, 1);		
			        }
		        }
		

		        i = i % Colmn;
		        // file tail
		        if(i != 0)
		        {
			        pbuf[0] = 0xff;	
			        for(; i < Colmn; i++)
				        BinaryToAscii(pbuf[0],AsciiBuf[i%perlength]);	
			        switch(Colmn)
			        {
				        case 4:
                            WriteHDataBuf(AsciiBuf[3], 0, 8);
                            WriteHDataBuf(AsciiBuf[2], 0, 8);
                            WriteHDataBuf(AsciiBuf[1], 0, 8);
                            WriteHDataBuf(AsciiBuf[0], 0, 8);
					        break;
				        case 2:
                            WriteHDataBuf(AsciiBuf[1], 0, 8);
                            WriteHDataBuf(AsciiBuf[0], 0, 8);
					        break;
				        case 1:
                            WriteHDataBuf(AsciiBuf[0], 0, 8);
					        break;
				        default:
					        break;
			        }
		        }
	        }

	        /*filelength = (UINT)sfile.GetLength();
	        if(filelength >= s_strChipDev.uintCfgBaseAddr)
	        {
		        pbuf='\n';
		        dfile.Write(&pbuf,1);
		        for(i = 0; i < 8; i++)
			        AsciiBuf[2][i] = 0x31;
		        AsciiBuf[3][0] = '0';
		        AsciiBuf[3][1] = '1';
		        AsciiBuf[3][2] = '0';
		        AsciiBuf[3][3] = '1';
		        AsciiBuf[3][4] = '1';
		        AsciiBuf[3][5] = '0';
		        AsciiBuf[3][6] = '1';
		        AsciiBuf[3][7] = '0';
		        // read config
		        sfile.Seek(s_strChipDev.uintCfgBaseAddr, 0);
		        i = sfile.Read(&pbuf, 1);
		        BinaryToAscii(pbuf,AsciiBuf[0]);
		        i = sfile.Read(&pbuf, 1);
		        BinaryToAscii(pbuf,AsciiBuf[1]);
		        dfile.Write(&AsciiBuf[1][bitstart], 8-bitstart);
		        dfile.Write(AsciiBuf[0], 8);
		        pbuf='\n';
		        dfile.Write(&pbuf,1);

		        dfile.Write(&AsciiBuf[2][bitstart], 8-bitstart);
		        dfile.Write(AsciiBuf[2], 8);
		        pbuf='\n';
		        dfile.Write(&pbuf,1);
		        dfile.Write(&AsciiBuf[2][bitstart], 8-bitstart);
		        dfile.Write(AsciiBuf[2], 8);
		        pbuf='\n';
		        dfile.Write(&pbuf,1);

		        dfile.Write(&AsciiBuf[2][bitstart], 8-bitstart);
		        dfile.Write(AsciiBuf[3], 8);

	        }*/

            s_HDataBufLength = s_HDataBufIndex;
            return true;
        }

        static void BinaryToAscii(byte inputData, char[] asciibuf)
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                if ((inputData & (0x1 << (7 - i))) > 0)
                {
                    asciibuf[i] = (char)0x31;
                }
                else
                {
                    asciibuf[i] = (char)0x30;
                }
            }
        }

        static char CheckSum(byte[] pBuf, char init, int len)
        {
            int i;
            char sum = init;
            for (i = 0; i < len; i++)
            {
                sum += (char)pBuf[i];
            }
            return sum;
        }

        static int ByteToChar(byte[] pInBuff, char[] pOutBuff, int len)
        {
            int i, j;
	        byte tmp;
	        char charout;
            for (i = 0, j = 0; i < len; i++)
            {

                tmp = pInBuff[i];
		        charout = (char)((tmp & 0xf0) >> 4);
		        if(charout <= 9)
                    charout += (char)0x30;
		        else
                    charout = (char)((charout - 9) + 0x40);
		        pOutBuff[j++] = charout;
		        charout = (char)(tmp & 0xf);
		        if(charout <= 9)
			        charout += (char)0x30;
		        else
                    charout = (char)((charout - 9) + 0x40);
                pOutBuff[j++] = charout;
	        }
	        return (len << 1);
        }

        static int CharToByte(char[] pInBuff, byte[] pOutBuff)
        {

            uint i, x;
            string strtmp;
            char[] pBuff = pInBuff;

            strtmp = new string(pBuff);
            strtmp = strtmp.ToUpper();
            pBuff = strtmp.ToArray();
            
            for (i = 0, x = 0; i < pInBuff.Length; i++)
            {
                if (((pInBuff[i] >= 'A') && (pInBuff[i] <= 'F'))
                    || ((pInBuff[i] >= '0') && (pInBuff[i] <= '9')))
                {
                    pBuff[x++] = pInBuff[i];
                }
            }

            for (i = 0, x = 0; i < pBuff.Length; i += 2)
            {
                byte y1, y2;
                if ((pBuff[i] >= '0') && (pBuff[i] <= '9'))
                    y1 = (byte)(pBuff[i] - '0');
                else
                    y1 = (byte)(pBuff[i] - 'A' + 10);

                if (0 == (byte)(pBuff[i + 1]))
                    y2 = 0;
                else if ((pBuff[i + 1] >= '0') && (pBuff[i + 1] <= '9'))
                    y2 = (byte)(pBuff[i + 1] - '0');
                else
                    y2 = (byte)(pBuff[i + 1] - 'A' + 10);

                pOutBuff[x++] = (byte)((y1 << 4) + y2);
            }

            pBuff = null;

            return (int)x;
        }

        static bool BinFileToBinArray(string HFileName)
        {
            FileStream sfile;

            try
            {
                sfile = new FileStream(HFileName, FileMode.Open);
            }
            catch
            {
                Console.WriteLine(">> xic2mic: Can not open BIN file!");
                return false;
            }

            uint filelength = (uint)sfile.Length;
            sfile.Position = 0;
            BinaryReader rsfile = new BinaryReader(sfile);

            s_DataBuf = rsfile.ReadBytes((int)filelength);
            s_DataBufLength = filelength;
            s_DataBufIndex = filelength;

            rsfile.Close();
            sfile.Close();

            return true;
        }
        static bool HexFileToBinArray(string HFileName)
        {	
	        FileStream sfile;
	        uint filelength;
	        char[] charbuf = new char[65];
	        byte datafieldlen;
	        byte[] bytebuf = new byte[33];
            byte[] bytetmp = new byte[4];
	        uint addrbefore;
	        uint startaddr;
	        byte fieldtype;
	        int i;
	        uint j;

	        // open source file
            try
            {
                sfile = new FileStream(HFileName, FileMode.Open);
            }
            catch
            {
                Console.WriteLine(">> xic2mic: Can not open HEX file!");
                return false;
            }
            filelength = (uint)sfile.Length;
	        sfile.Position = 0;
	        addrbefore = 0;
            BinaryReader rsfile = new BinaryReader(sfile);

	        while(sfile.Position != filelength)
	        {
		        // get ':'
                charbuf = rsfile.ReadChars(1);
		        
		        if(charbuf[0] == ':')
		        {
			        // get data lenth;
                    charbuf = rsfile.ReadChars(2);
			        //charbuf[2] = (char)0;
			        CharToByte(charbuf, bytetmp);
                    datafieldlen = bytetmp[0];
                    if(datafieldlen > 0x10)
                    {
                        Console.WriteLine(">> xic2mic: HEX file data line is too long");
                        return false;
                    }

			        // get start address
                    charbuf = rsfile.ReadChars(4);
			        //charbuf[4] = (char)0;
			        CharToByte(charbuf, bytetmp);
                    startaddr = (uint)((bytetmp[0] * 256) + bytetmp[1]);
			        
			        // get the field type
			        charbuf = rsfile.ReadChars(2);
			        //charbuf[2] = (char)0;
			        CharToByte(charbuf, bytetmp);
                    fieldtype = bytetmp[0];
			
			        // space
			        if(addrbefore <= startaddr)
			        {
				        //dfile.Seek(addrbefore, CFile::begin);
				        s_DataBufIndex = addrbefore;
				
				        uint bsslen = (startaddr - addrbefore);
				        uint bsslensec;
				        while(bsslen>0)
				        {
					        if(bsslen > 512)
						        bsslensec = 512;
					        else
						        bsslensec = bsslen;
					        bsslen -= bsslensec;

					        for(i = 0; i < bsslensec; i++)
							        s_DataBuf[s_DataBufIndex++] = (byte)cmdInfo.fillCharacter;
					        //dfile.Write(databuf, bsslensec);
				        }
				        addrbefore = startaddr + datafieldlen;
			        }
			        else if(addrbefore > startaddr)
			        {
				        //dfile.Seek(startaddr, CFile::begin);
				        s_DataBufIndex = startaddr;
				        if(startaddr + datafieldlen > addrbefore)
					        addrbefore = startaddr + datafieldlen;
			        }


			        // read data
			        if(datafieldlen != 0x0)
			        {
				        //sfile.Read(databuf,datafieldlen * 2);
                        charbuf = rsfile.ReadChars(datafieldlen * 2);
				        //charbuf[datafieldlen * 2] = (char)0;
				
				        CharToByte(charbuf, bytebuf);	
				        // write data to the destination file
				        //dfile.Write(databuf, datafieldlen);
				        for(i = 0; i < datafieldlen; i++)
					        s_DataBuf[s_DataBufIndex++] = bytebuf[i];
			        }
		        }
	        }
	        if(cmdInfo.fillFileEnable == true)
	        {
		        if(addrbefore < chipDev.uintBFileLenTop)
		        {
			        s_DataBufIndex = addrbefore;
			        for(j = addrbefore; j < chipDev.uintBFileLenTop; j++)
				        s_DataBuf[s_DataBufIndex++] = 0xff;
			        s_DataBufLength = j;
		        }
		        else
			        s_DataBufLength = addrbefore;
	        }
	        else
		        s_DataBufLength = addrbefore;
            rsfile.Close();
	        sfile.Close();

	        return true;
        }

        static void Help()
        {
            Console.WriteLine("xic2mic: Convert code from PIC, 78X153, 8PX53 to MIC8S");
            Console.WriteLine("Usage: xic2mic.exe -f file [-cx {source_core_name}] [--ma]......");
            Console.WriteLine("Options:");
            Console.Write(" -f file\t\t");
            Console.WriteLine("specify source file, support binary or hex.\n");


            Console.Write(" -cx src_arch");
            Console.WriteLine("\tspecify the source chip:");
            Console.WriteLine("\t\t\tsrc_arch = PIC14, PIC12, 78X153, 8PX53");
            Console.WriteLine("\t\t\tdefault for PIC14 if src_arch not given.");

            Console.Write(" -cm mic_arch");
            Console.WriteLine("\tspecify the target chip architecture:");
            Console.WriteLine("\t\t\tmic_arch = MIC8S.");
            Console.WriteLine("\t\t\tonly support MIC8S by now.\n");


            Console.Write(" --xb");
            Console.WriteLine("\t\t\tgenerate bin file from the source hex file.");

            Console.Write(" --mb");
            Console.WriteLine("\t\t\tgenerate bin file for mic8s.");

            Console.Write(" --mh");
            Console.WriteLine("\t\t\tgenerate hex file for mic8s.");

            Console.Write(" --ma");
            Console.WriteLine("\t\t\tgenerate abin file for mic8s.");

            Console.Write(" --fd");
            Console.WriteLine("\t\t\tdisable byte fill.");
            Console.WriteLine("\t\t\tdefault: convert will try to fill unused memory region");
            Console.Write(" --fz");
            Console.WriteLine("\t\t\tfill unused region by 0x0: default value is 0xff if [--fz] not specified.\n");


            Console.Write(" --h");
            Console.WriteLine("\t\t\tDisplay This information.");
        }
    }
}
