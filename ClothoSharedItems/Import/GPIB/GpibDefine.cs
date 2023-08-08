using System;

namespace ClothoSharedItems.Import.GPIB
{
    // Timeout values and meanings
    public enum GpibTimeout : int
    {
        None = 0,                        /* Infinite timeout (disabled)         */
        T10us = 1,                       /* Timeout of 10 us (ideal)            */
        T30us = 2,                       /* Timeout of 30 us (ideal)            */
        T100us = 3,                      /* Timeout of 100 us (ideal)           */
        T300us = 4,                      /* Timeout of 300 us (ideal)           */
        T1ms = 5,                        /* Timeout of 1 ms (ideal)             */
        T3ms = 6,                        /* Timeout of 3 ms (ideal)             */
        T10ms = 7,                       /* Timeout of 10 ms (ideal)            */
        T30ms = 8,                       /* Timeout of 30 ms (ideal)            */
        T100ms = 9,                      /* Timeout of 100 ms (ideal)           */
        T300ms = 10,                     /* Timeout of 300 ms (ideal)           */
        T1s = 11,                        /* Timeout of 1 s (ideal)              */
        T3s = 12,                        /* Timeout of 3 s (ideal)              */
        T10s = 13,                       /* Timeout of 10 s (ideal)             */
        T30s = 14,                       /* Timeout of 30 s (ideal)             */
        T100s = 15,                      /* Timeout of 100 s (ideal)            */
        T300s = 16,                      /* Timeout of 300 s (ideal)            */
        T1000s = 17,                     /* Timeout of 1000 s (ideal)           */
    }

    // Status bit vector : global variable ibsta and wait mask
    [Flags]
    public enum GpibStatus : int
    {
        None = 0,
        DeviceClear = (1 << 0),             //1,        /* Device clear state                   */
        DeviceTrigger = (1 << 1),           //2,        /* Device trigger state                 */
        ListenerAddressed = (1 << 2),       //4,        /* Listener active                      */
        TalkerAddressed = (1 << 3),         //8,        /* Talker active                        */
        Attention = (1 << 4),               //16,       /* Attention asserted                   */
        ControllerInCharge = (1 << 5),      //32,       /* Controller-in-Charge                 */
        Remote = (1 << 6),                  //64,       /* Remote state                         */
        Lockout = (1 << 7),                 //128,      /* Local lockout state                  */
        IOComplete = (1 << 8),              //256,      /* I/O completed                        */
        DeviceServiceRequest = (1 << 11),   //2048,     /* Device needs service                 */
        ServiceRequest = (1 << 12),         //4096,     /* SRQ detected by CIC                  */
        End = (1 << 13),                    //8192,     /* EOI or EOS detected                  */
        Timeout = (1 << 14),                //16384,    /* Timeout                              */
        Error = (1 << 15),                                 /* Error detected                       */
        //Error = -32768,                                 /* Error detected                       */
    }

    // Error messages returned in global variable iberr
    public enum GpibError : int
    {
        SystemError = 0,                                            /* System error                            */
        GpibBoardNotControllerInCharge = 1,                         /* Function requires GPIB board to be CIC  */
        NoListenersDetected = 2,                                    /* Write function detected no Listeners    */
        BoardNotAddressedCorrectly = 3,                             /* Interface board not addressed correctly */
        InvalidArgument = 4,                                        /* Invalid argument to function call       */
        GpibBoardNotSystemController = 5,                           /* Function requires GPIB board to be SAC  */
        IOOperationAborted = 6,                                     /* I/O operation aborted                   */
        NonExistentInterfaceBoard = 7,                              /* Non-existent interface board            */
        DmaNotAvailable = 8,                                        /* Error performing DMA                    */
        AsynchronousIOInProgress = 10,                              /* I/O operation started before previous operation completed                     */
        DriverFeatureNotAvailable = 11,                             /* No capability for intended operation    */
        FileSystemOperationError = 12,                              /* File system operation error             */
        CommandErrorDuringDeviceCall = 14,                          /* Command error during device call        */
        SerialPollQueueOverflow = 15,                               /* Serial poll status byte lost            */
        StuckSrq = 16,                                              /* SRQ remains asserted                    */
        TableProblem = 20,                                          /* The return buffer is full.              */
        AddressOrBoardIsLocked = 21,                                /* Address or board is locked.             */
        NotifyCallbackFailedToRearm = 22,                           /* The ibnotify Callback failed to rearm   */
        InvalidInputHandle = 23,                                    /* The input handle is invalid             */
        WaitAlreadyInProgressOnUnitDescriptor = 26,                 /* Wait already in progress on input ud    */
        EventNotificationCancelledDueToResetOfInterface = 27,       /* The event notification was cancelled due to a reset of the interface         */
        InterfaceLostPower = 28,                                    /* The system or board has lost power or gone to standby                         */
    }

    public enum Gpib64Option : int
    {
        //
        // The following constants are used for the second parameter of the
        // ibconfig function.  They are the "option" selection codes.
        //
        IbcPAD = 0x0001,  // Primary Address

        IbcSAD = 0x0002,  // Secondary Address
        IbcTMO = 0x0003,  // Timeout Value
        IbcEOT = 0x0004,  // Send EOI with last data byte?
        IbcPPC = 0x0005,  // Parallel Poll Configure
        IbcREADDR = 0x0006,  // Repeat Addressing
        IbcAUTOPOLL = 0x0007,  // Disable Auto Serial Polling
        IbcSC = 0x000A,  // Board is System Controller?
        IbcSRE = 0x000B,  // Assert SRE on device calls?
        IbcEOSrd = 0x000C,  // Terminate reads on EOS
        IbcEOSwrt = 0x000D,  // Send EOI with EOS character
        IbcEOScmp = 0x000E,  // Use 7 or 8-bit EOS compare
        IbcEOSchar = 0x000F,  // The EOS character
        IbcPP2 = 0x0010,  // Use Parallel Poll Mode 2
        IbcTIMING = 0x0011,  // NORMAL, HIGH, or VERY_HIGH timing
        IbcDMA = 0x0012,  // Use DMA for I/O
        IbcSendLLO = 0x0017,  // Enable/disable the sending of LLO
        IbcSPollTime = 0x0018,  // Set the timeout value for serial polls
        IbcPPollTime = 0x0019,  // Set the parallel poll length period
        IbcEndBitIsNormal = 0x001A,  // Remove EOS from END bit of IBSTA
        IbcUnAddr = 0x001B,  // Enable/disable device unaddressing
        IbcHSCableLength = 0x001F,  // Length of cable specified for high speed timing
        IbcIst = 0x0020,  // Set the IST bit
        IbcRsv = 0x0021,  // Set the RSV byte
        IbcLON = 0x0022,  // Enter listen only mode
        IbcEOS = 0x0025,  // Macro for ibeos
    }
}