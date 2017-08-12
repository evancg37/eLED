//eLED V1.0 - Published 8/11/2017 by Evan Greavu
//Usage - instantiate an eLED object, then use the commands on that object:
//eLED.turnOn(zone); eLED.turnOff(zone); eLED.setBrightnesS(zone, brightness); eLED.setColor(zone, color);


using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace eLED
{
    class eLED
    {
        private Controller control;

        public eLED(string ip)
        {
            control = new Controller(ip);
            this.turnOn(0);
            control.lightsOn = true;
        }

        public void turnOn(int zone)
        {
            if (!control.lightsOn)
            {
                byte zoneByte = (byte)zone;
                control.lightsOn = true;
                execute(Controller.lightOn, zoneByte);
            }
        }

        public void turnOff(int zone)
        {
            if (control.lightsOn)
            {
                byte zoneByte = (byte)zone;
                control.lightsOn = false;
                execute(Controller.lightOff, zoneByte);
            }
        }

        public void nightMode(int zone)
        {
            if (control.lightsOn == false)
            {
                turnOn(zone);
                control.lightsOn = true;

            }
            byte zoneByte = (byte)zone;
            execute(Controller.nightMode, zoneByte);
        }

        public void setBrightness(int zone, float f)
        {
            if (f != control.lastBrightness)
            {
                byte zoneByte = (byte)zone;
                var brightCom = Controller.brightSet.ToArray();
                brightCom[5] = (byte)f;
                execute(brightCom, zoneByte);

            }
            control.lastBrightness = f;
        }

        public void whiteMode(int zone)
        {
            if (control.lightsOn == false)
            {
                turnOn(zone);
                control.lightsOn = true;
            }

            if (control.lastColor != -1)
            {
                byte zoneByte = (byte)zone;
                execute(Controller.whiteSet, zoneByte);
            }
            control.lastColor = -1;
            setBrightness(zone, control.lastBrightness);
        }

        public void setColor(int zone, float f)
        {
            if (control.lightsOn == false)
            {
                turnOn(zone);
                control.lightsOn = true;
            }

            if (control.lastColor != f)
            {
                byte zoneByte = (byte)zone;
                var colorCom = Controller.colorSet.ToArray();
                colorCom[5] = (byte)f; colorCom[6] = (byte)f; colorCom[7] = (byte)f; colorCom[8] = (byte)f;
                execute(colorCom, zoneByte);
            }
            control.lastColor = f;
            setBrightness(zone, control.lastBrightness);
        }

        public void setMode(int zone, int mode)
        {
            if (control.lightsOn == false)
            {
                turnOn(zone);
                control.lightsOn = true;
            }

            byte zoneByte = (byte)zone;
            var modeCom = Controller.setMode.ToArray();
            modeCom[5] = (byte)mode;
            execute(modeCom, zoneByte);
        }

        public void speedDown(int zone)
        {
            if (control.lightsOn == false)
            {
                turnOn(zone);
                control.lightsOn = true;
            }

            byte zoneByte = (byte)zone;
            execute(Controller.speedDown, zoneByte);
        }

        private void execute(byte[] command, byte zone)
        {
            var commandBytes = control.assembleCommand(command, zone);
            var response = control.sendCommand(commandBytes);

            if (response.Length != 8)
            {
                Console.WriteLine("Communication with LED controller failed.");
            }
            else if (response[7] != 0x00)
            {
                Console.WriteLine("Session expired, creating new session...");
                control.needsToConnect = true;
                control.startSession();

                commandBytes = control.assembleCommand(command, zone);
                control.sendCommand(commandBytes);
            }

        }
    }

    class Controller
    {

        #region controller settings

        static bool _DEBUG = true;

        private static byte[] sessionRequest = new byte[] { 0x20, 0x00, 0x00, 0x00, 0x16, 0x02, 0x62,
                                                    0x3A, 0xD5, 0xED, 0xA3, 0x01, 0xAE, 0x08,
                                                    0x2D, 0x46, 0x61, 0x41, 0xA7, 0xF6, 0xDC,
                                                    0xAF, 0xD3, 0xE6, 0x00, 0x00, 0x1E };

        private static byte[] preamble = new byte[] { 0x80, 0x00, 0x00, 0x00, 0x11 };

        public static byte[] lightOn = new byte[]   { 0x31, 0x00, 0x00, 0x07, 0x03, 0x01, 0x00, 0x00, 0x00 };
        public static byte[] lightOff = new byte[]  { 0x31, 0x00, 0x00, 0x07, 0x03, 0x02, 0x00, 0x00, 0x00 };
        public static byte[] colorSet = new byte[]  { 0x31, 0x00, 0x00, 0x07, 0x01, 0xEE, 0xEE, 0xEE, 0xEE }; //EE, EE, EE, EE for RGB in hex
        public static byte[] brightSet = new byte[] { 0x31, 0x00, 0x00, 0x07, 0x02, 0xEE, 0x00, 0x00, 0x00 }; //EE for brightness
        public static byte[] setMode = new byte[]   { 0x31, 0x00, 0x00, 0x07, 0x04, 0xEE, 0x00, 0x00, 0x00 }; //EE for mode
        public static byte[] speedDown = new byte[] { 0x31, 0x00, 0x00, 0x07, 0x03, 0x04, 0x00, 0x00, 0x00 };
        public static byte[] whiteSet = new byte[]  { 0x31, 0x00, 0x00, 0x07, 0x03, 0x05, 0x00, 0x00, 0x00 };
        public static byte[] nightMode = new byte[] { 0x31, 0x00, 0x00, 0x07, 0x03, 0x06, 0x00, 0x00, 0x00 };

        public static byte Zone1 = 0x01;
        public static byte Zone2 = 0x02;
        public static byte Zone3 = 0x03;
        public static byte Zone4 = 0x04;
        public static byte allZone = 0x00;

        #endregion

        #region net settings
        public int port = 5987;

        private byte sessionID1;
        private byte sessionID2;
        public byte getSessionID1() { return sessionID1; }
        public byte getSessionID2() { return sessionID2; }

        private byte sequenceNum = 0x01;
        public byte getSequenceNum() { return sequenceNum; }

        public bool needsToConnect = true;
        public bool lightsOn = false;
        public float lastColor = -1;
        public float lastBrightness = 100;

        UdpClient udpClient;
        IPEndPoint remotePoint = new IPEndPoint(IPAddress.Any, 5987);

        #endregion

        public Controller(string ip)
        {
            log("Initializing new controller on IP " + ip + "...");

            udpClient = new UdpClient();
            udpClient.Client.SendTimeout = 2000;
            udpClient.Client.ReceiveTimeout = 1000;
            udpClient.Connect(ip, port);
            log("UDP Client initiated.");

            startSession();
        }

        public void startSession()
        {
            sequenceNum = 0x01;
            udpClient.Send(sessionRequest, sessionRequest.Length);

            log("Sent session request to " + IP + ":" + port);

            var response = udpClient.Receive(ref remotePoint);
            log("\nReceived response from server:");
            log(BitConverter.ToString(response).Replace("-", " "));

            sessionID1 = response[19];
            sessionID2 = response[20];
            log("\nWifiBridgesession1: " + sessionID1.ToString("X2"));
            log("WifiBridgesession2: " + sessionID2.ToString("X2"));
            sequenceNum += 0x01;

            Console.WriteLine("WifiBridge session started successfully.\n");
            needsToConnect = false;

        }

        public byte[] assembleCommand(byte[] action, byte zone)
        {
            byte checksum = computeChecksum(action, zone);

            byte[] concat = new byte[22];
            System.Buffer.BlockCopy(preamble, 0, concat, 0, preamble.Length);

            byte[] commandMeta = { 0x00, 0x00, 0x00, 0x00, 0x00 };
            commandMeta[0] = sessionID1; commandMeta[1] = sessionID2;  commandMeta[3] = sequenceNum;
            System.Buffer.BlockCopy(commandMeta, 0, concat, 5, commandMeta.Length);

            System.Buffer.BlockCopy(action, 0, concat, 10, action.Length);

            byte[] zoneChecksum = { 0x00, 0x00, 0x00 };
            zoneChecksum[0] = zone; zoneChecksum[2] = checksum;
            System.Buffer.BlockCopy(zoneChecksum, 0, concat, 19, zoneChecksum.Length);

            return concat;
        }

        public byte[] sendCommand(byte[] command)
        {
            log("Sending command " + BitConverter.ToString(command).Replace("-", " "));
            int timeout = 3;

            while (timeout-- > 0)
            {
                try
                {
                    udpClient.Send(command, command.Length);
                }
                catch (SocketException ex) { }
            }

            timeout = 3;
            byte[] response = { };

            while (timeout-- > 0)
            {
                try
                {
                    response = udpClient.Receive(ref remotePoint);
                    break;
                }
                catch(SocketException ex) { }
            }

            return response;
        }

        public byte computeChecksum(byte[] command, byte zone)
        {
            byte total = 0x00;

            foreach (byte b in command)
            {
                total += b;
            }
            total += zone;

            return total;
        }

        private static void log(string e)
        {
            if (_DEBUG) Console.WriteLine(e);
        }
    }

}
