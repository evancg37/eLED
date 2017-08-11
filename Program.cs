using System;
using System.Threading;
using CSCore.CoreAudioAPI;

namespace eLED
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Initializing eLED server.");
            int frequency = 20;

            string input;
            int session;
            int zone;
            int mode;

            Console.WriteLine("Select zone to begin...\n1-Evan\n2-Family room\n3-?\n4-?\n");
            input = Console.ReadLine();

            int.TryParse(input, out zone);

            if (!(zone == 0 || zone == 1 || zone == 2 || zone == 3 || zone == 4))
                zone = 0;

            Console.WriteLine("\n\nSelect mode...\n1-Video matcher\n2-Volume dancer\n3-Party player\n");
            input = Console.ReadLine();

            int.TryParse(input, out mode);

            if (!(mode == 1 || mode == 2 || mode == 3))
                mode = 1;

            Console.WriteLine("\nPlease specify an audio session:\n");
            input = Console.ReadLine();

            int.TryParse(input, out session);

            Console.WriteLine("\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n");

            eLED eLED = new eLED();
            eLED.turnOn(zone);
            eLED.setColor(zone, 0);
            eLED.setBrightness(zone, 100);
            eLED.whiteMode(zone);
            eLED.setBrightness(zone, 100);

            if (mode == 1)
            {
                VideoMatcher(eLED, zone, frequency);
            }
            else if (mode == 2 || mode == 3)
            {

                if (mode == 2)
                    VolumeDancer(eLED, zone, session, frequency, true);
                else if (mode == 3)
                    PartyPlayer(eLED, zone, session, frequency, false);
            }

        }

        public static void VideoMatcher(eLED eLED, int zone, int frequency)
        {
            var matcher = new ScreenMatcher();

            new Thread(() =>
            {
                matcher.startWatching();
            }).Start();


            while (true)
            {
                Thread.Sleep(1000 / frequency);

                if (matcher.color == -99)
                    eLED.turnOff(zone);
                else if (matcher.color == -1)
                    eLED.whiteMode(zone);
                else
                    eLED.setColor(zone, matcher.color);

            }
        }

        public static void VolumeDancer(eLED eLED, int zone, int _session, int frequency, bool quietOff=true)
        {
            float[] peaks = new float[4];

            AudioSessionManager2 session;
            AudioSessionEnumerator sessionEnumerator;
            AudioSessionControl program;

            var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            session = AudioSessionManager2.FromMMDevice(device);

            int chosenSession = 0;

            while (true)
            {
                Thread.Sleep(1000 / frequency);

                if (session.IsDisposed)
                    session = AudioSessionManager2.FromMMDevice(device);

                sessionEnumerator = session.GetSessionEnumerator();

                program = sessionEnumerator.GetSession(chosenSession);

                using (var audioMeterInformation = program.QueryInterface<AudioMeterInformation>())
                {
                    peaks[0] = audioMeterInformation.GetPeakValue() * 100f;
                }

                if (peaks[0] < 12 && quietOff)
                {
                    eLED.turnOff(zone);
                }
                else
                {
                    eLED.turnOn(zone);
                    eLED.setBrightness(zone, peaks[0]);
                }

                sessionEnumerator.Dispose();
                device.Dispose();
                session.Dispose();
            }
        }

        public static void PartyPlayer(eLED eLED, int zone, int audioSession, int frequency, bool quietOff=false)
        {
            new Thread(() =>
            {
                VideoMatcher(eLED, zone, frequency);
            }).Start();

            VolumeDancer(eLED, zone, audioSession, frequency, quietOff);
        }
    }


}
