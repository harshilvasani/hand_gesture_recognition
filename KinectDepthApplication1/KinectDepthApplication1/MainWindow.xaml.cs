/////problem=won’t work if hand comes between kinect and shoulder

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Windows.Controls;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO.Ports;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace KinectDepthApplication1
{
    public partial class MainWindow : Window
    {
        //Instantiate the Kinect runtime. Required to initialize the device.
        //IMPORTANT NOTE: You can pass the device ID here, in case more than one Kinect device is connected.

        KinectSensor sensor;// = KinectSensor.KinectSensors.First();

        Skeleton[] skeletons;
        Microsoft.Kinect.SkeletonPoint vector1;
        Microsoft.Kinect.SkeletonPoint vector2;


        Line l;
        short[] pixelData;
        DepthImagePixel[] depthPixels;

        bool[,] boundary_X_Y = new bool[640, 480]; //only square part used
        bool[,] valid = new bool[640, 480];        //only square part used

        int[,] sort_bound;                         //array to store sorted boundary
        int[,] fingers;                            //array to store coordinated of finger-tip's pixel

        int sbi = -1;                              // index for sorted boundary pixel
        int bi = 0;                                  // counter for boundary pixels
        int fi, fi_1, fi_2;                        //last two are counters for fingers of both hand
        int start_i = -1, start_j = -1;                //starting pixel coordinate of boundary pixel

        double f1_sum, f2_sum;

        int[] button_cnt = new int[29];            //counter array for all gestures

        int frame_cnt = 0;
        const int max_frm = 10;

        int hand_total_x = 0, hand_total_y = 0;
        int X1, Y1, X2, Y2;                        //coordinates for palm of both hands

        string[] remote = { " ", "+", "-", "*", "/", "X", "0", 
                            "1", "2", "3", "4", "5", "6", "7", 
                            "8", "9", "P", "E", "^", "v", "<", 
                            ">", "o", "F", "L", "G", "M", "R", "B" };

        SerialPort serialPort1 = new SerialPort();


        public MainWindow()
        {
            for (int i = 0; i < 29; i++)
            {
                button_cnt[i] = 0;
            }



            //remove this comment after review 3
            /* find_port();
            
             if (comport_found == false)
             {
                 this.Close();//closes the main window
             }
             */
            // if (comport_found == true)
            {
                sensor = KinectSensor.KinectSensors.First();
                InitializeComponent();

                //Runtime initialization is handled when the window is opened. When the window
                //is closed, the runtime MUST be unitialized.
                this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
                this.Unloaded += new RoutedEventHandler(MainWindow_Unloaded);

                sensor.DepthStream.Enable();
                sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);


                sensor.SkeletonStream.Enable();
                sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;// Use Seated Mode
                sensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(runtime_SkeletonFrameReady);
            }
        }

        void sensor_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            bool receivedData = false;

            using (DepthImageFrame DFrame = e.OpenDepthImageFrame())
            {
                if (DFrame == null)
                {
                    // The image processing took too long. More than 2 frames behind.
                    Console.WriteLine("DFrame is NULL");
                }
                else
                {
                    pixelData = new short[DFrame.PixelDataLength];
                    DFrame.CopyPixelDataTo(pixelData);

                    depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];

                    DFrame.CopyDepthImagePixelDataTo(depthPixels);

                    if (frame_cnt != max_frm)
                    {
                        frame_cnt++;
                    }
                    else
                    {
                        frame_cnt = 0;
                        f1_sum = 0;
                        f2_sum = 0;
                    }

                    set_boundary(DFrame);

                    receivedData = true;
                }
            }

            if (receivedData)
            {
                BitmapSource source = BitmapSource.Create(640, 480, 96, 96,
                        PixelFormats.Gray16, null, pixelData, 640 * 2);

                depthImage.Source = source;
            }
        }

        void runtime_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            bool receivedData = false;

            using (SkeletonFrame SFrame = e.OpenSkeletonFrame())
            {
                if (SFrame == null)
                {
                    // The image processing took too long. More than 2 frames behind.
                    Console.WriteLine("SFrame is NULL");
                }
                else
                {
                    skeletons = new Skeleton[SFrame.SkeletonArrayLength];
                    SFrame.CopySkeletonDataTo(skeletons);
                    receivedData = true;
                }
            }

            if (receivedData)
            {

                Skeleton currentSkeleton = (from s in skeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked
                                            select s).FirstOrDefault();

                if (currentSkeleton != null)
                {


                    Joint joint2 = currentSkeleton.Joints[JointType.ShoulderCenter];

                    vector2 = new Microsoft.Kinect.SkeletonPoint();
                    vector2.X = ScaleVector(640, joint2.Position.X);
                    vector2.Y = ScaleVector(480, -joint2.Position.Y);

                    Joint updatedJoint2 = new Joint();
                    updatedJoint2 = joint2;
                    updatedJoint2.TrackingState = JointTrackingState.Tracked;
                    updatedJoint2.Position = vector2;

                    Canvas.SetLeft(head, vector2.X);
                    Canvas.SetTop(head, vector2.Y);
                }

            }
        }
        /*-----------------------------------------------------------------------------------------*/
        /****************************************My Function****************************************/

        int D2;
        bool shoulder_found = false;
        int cnt = 0;

        bool comport_found = false;
        String send_ch = "s";
        char[] recieve_ch = new char[1];
        int limit;
        DialogResult result;

        void find_port()
        {
            Console.WriteLine("-------------------------------------------------------------------");

            recieve_ch = send_ch.ToCharArray();

            serialPort1.BaudRate = 9600;

            String[] port_name = SerialPort.GetPortNames();
            serialPort1.Close();

            int[] not_used = new int[port_name.Length];


            limit = 0;
            while (comport_found == false && limit <= 2)
            {
                limit++;
                for (int i = 0; i < port_name.Length; i++)
                {
                    serialPort1.Close();
                    if (not_used[i] == 0)
                    {
                        serialPort1.PortName = port_name[i];

                        try
                        {
                            serialPort1.Open();
                            if (serialPort1.IsOpen)
                            {

                                serialPort1.Write(send_ch);
                                char[] data = new char[1];
                                try
                                {
                                    serialPort1.ReadTimeout = 5000;
                                    serialPort1.Read(data, 0, data.Length);//working as data.length == 1

                                    if (data[0] == recieve_ch[0])
                                    {
                                        comport_found = true;
                                        Console.WriteLine(data[0] + " " + serialPort1.PortName);
                                        serialPort1.Close();
                                        Console.WriteLine(limit);

                                        break;
                                    }

                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(port_name[i] + " not correct port");

                                }

                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(port_name[i] + " not in use ");
                            not_used[i] = 1;
                        }

                    }
                }
            }


            if (comport_found == false)
            {
                // Console.WriteLine("COM port not found");
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;

                result = System.Windows.Forms.MessageBox.Show("COM port not found\nwant search again", " ", buttons);

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    find_port();
                }
            }
        }

        void set_boundary(DepthImageFrame DFrame)
        {
            if (!(vector2.X == 0 && vector2.Y == 0) && shoulder_found == false)
            {
                shoulder_found = true;
                D2 = (ushort)pixelData[(int)vector2.X + (int)vector2.Y * DFrame.Width];//by me
                D2 = D2 >> 3;
            }

            if (cnt == 15)//find shoulder depth again
            {
                D2 = (ushort)pixelData[(int)vector2.X + (int)vector2.Y * DFrame.Width];//by me
                D2 = D2 >> 3;
                cnt = 0;
            }

            if (!(vector2.X == 0 && vector2.Y == 0) && shoulder_found == true)
            {

                cnt++;
                //////
                int i, j;
                fi_1 = -1;
                fi_2 = -1;

                for (i = 0; i < 640; i++)
                {
                    for (j = 0; j < 480; j++)
                    {
                        valid[i, j] = false;
                        boundary_X_Y[i, j] = false;
                    }
                }

                start_i = -1;
                start_j = -1;
                bi = 0;

                for (i = 180; i < 470; i++)//to cover whole image set i=0 & i<640
                {
                    for (j = 160; j < 330; j++)//to cover whole image set j=0 & i<480
                    {
                        int d = (ushort)pixelData[i + j * DFrame.Width];//by me
                        d = d >> 3;//by me
                        if (d <= D2 - 150)//for square use 1200
                        {
                            valid[i, j] = true;
                        }
                    }
                }

                for (i = 180; i < 470; i++)//to cover whole image set i=0 & i<640
                {
                    for (j = 160; j < 330; j++)//to cover whole image set j=0 & i<480
                    {
                        {
                            int d;
                            if (valid[i, j] == true)//for square use 1200
                            {
                                if (((valid[i - 1, j] == true && valid[i + 1, j] == false) || (valid[i - 1, j] == false && valid[i + 1, j] == true) || (valid[i, j - 1] == false && valid[i, j + 1] == true) || (valid[i, j - 1] == true && valid[i, j + 1] == false)) && valid[i, j])
                                {
                                    if (start_i == -1 && start_j == -1 && j == 329)//need to set start from wrist coordinate
                                    {
                                        start_i = i;
                                        start_j = j;

                                    }
                                    d = 7000;
                                    d = d << 3;
                                    pixelData[i + j * DFrame.Width] = (short)d;
                                    boundary_X_Y[i, j] = true;
                                    bi++;
                                }

                                else
                                {
                                    d = 7000;
                                    d = d << 3;
                                    pixelData[i + j * DFrame.Width] = (short)d;
                                }
                            }

                            else if (!boundary_X_Y[i, j] && j == 329 && start_i != -1 && start_j != -1)//makes additional line
                            {
                                d = 7000;
                                d = d << 3;
                                pixelData[i + j * DFrame.Width] = (short)d;
                                boundary_X_Y[i, j] = true;
                                bi++;
                            }

                            else
                            {
                                d = 7000;
                                d = d << 3;
                                pixelData[i + j * DFrame.Width] = (short)(d);
                            }
                        }
                    }
                }

                sort(DFrame.Width);
                gesture(fi_1, fi_2);
            }
        }

        void sort(int DD)
        {
            if (start_i != -1 && start_j != -1 && bi > 0)
            {
                sort_bound = new int[bi + 1, 2];
                int cur_i = start_i; //180;//180,160 has taken as start OR
                int cur_j = start_j; //160;//use first boundary pixiel
                sbi = -1;
                int cnt = 0;

                do
                {
                    ++sbi;
                    sort_bound[sbi, 0] = cur_i;//.Add(new PointFT(current.X, current.Y));
                    sort_bound[sbi, 1] = cur_j;
                    boundary_X_Y[cur_i, cur_j] = false;
                    if (boundary_X_Y[cur_i - 1, cur_j - 1])
                    {
                        cur_i -= 1;
                        cur_j -= 1;
                    }
                    else if (boundary_X_Y[cur_i, cur_j - 1])
                    {
                        cur_j -= 1;
                    }
                    else if (boundary_X_Y[cur_i + 1, cur_j - 1])
                    {
                        cur_i += 1;
                        cur_j -= 1;
                    }
                    else if (boundary_X_Y[cur_i + 1, cur_j])
                    {
                        cur_i += 1;
                    }
                    else if (boundary_X_Y[cur_i + 1, cur_j + 1])
                    {
                        cur_i += 1;
                        cur_j += 1;
                    }
                    else if (boundary_X_Y[cur_i, cur_j + 1])
                    {
                        cur_j += 1;
                    }
                    else if (boundary_X_Y[cur_i - 1, cur_j + 1])
                    {
                        cur_i -= 1;
                        cur_j += 1;
                    }
                    else if (boundary_X_Y[cur_i - 1, cur_j])
                    {
                        cur_i -= 1;
                    }
                    else if (sbi < bi)
                    {
                        if (boundary_X_Y[cur_i - 2, cur_j - 2])
                        {
                            cur_i -= 2;
                            cur_j -= 2;
                        }
                        else if (boundary_X_Y[cur_i - 1, cur_j - 2])
                        {
                            cur_i -= 1;
                            cur_j -= 2;
                        }
                        else if (boundary_X_Y[cur_i, cur_j - 2])
                        {
                            // cur_i += 1;
                            cur_j -= 2;
                        }
                        else if (boundary_X_Y[cur_i + 1, cur_j - 2])
                        {
                            cur_i += 1;
                            cur_j -= 2;
                        }
                        else if (boundary_X_Y[cur_i + 2, cur_j - 2])
                        {
                            cur_i += 2;
                            cur_j -= 2;
                        }
                        else if (boundary_X_Y[cur_i + 2, cur_j - 1])
                        {
                            cur_i += 2;
                            cur_j -= 1;
                        }
                        else if (boundary_X_Y[cur_i + 2, cur_j])
                        {
                            cur_i += 2;
                            //cur_j += 1;
                        }
                        else if (boundary_X_Y[cur_i + 2, cur_j + 1])
                        {
                            cur_i += 2;
                            cur_j += 1;
                        }
                        else if (boundary_X_Y[cur_i + 2, cur_j + 2])
                        {
                            cur_i += 2;
                            cur_j += 2;
                        }
                        else if (boundary_X_Y[cur_i + 1, cur_j + 2])
                        {
                            cur_i += 1;
                            cur_j += 2;
                        }
                        else if (boundary_X_Y[cur_i, cur_j + 2])
                        {
                            cur_j += 2;
                        }
                        else if (boundary_X_Y[cur_i - 1, cur_j + 2])
                        {
                            cur_i -= 1;
                            cur_j += 2;
                        }
                        else if (boundary_X_Y[cur_i - 2, cur_j + 2])
                        {
                            cur_i -= 2;
                            cur_j += 2;
                        }
                        else if (boundary_X_Y[cur_i - 2, cur_j + 1])
                        {
                            cur_i -= 2;
                            cur_j += 1;
                        }
                        else if (boundary_X_Y[cur_i - 2, cur_j])
                        {
                            cur_i -= 2;

                        }
                        else if (boundary_X_Y[cur_i - 2, cur_j - 1])
                        {
                            cur_i -= 2;
                            cur_j -= 1;
                        }

                        else
                            cnt++;
                    }
                    // */  
                } while (sbi < bi);

                get_hands(DD);
            }
        }

        void get_hands(int DD)
        {
            hand_total_x = 0;
            hand_total_y = 0;

            int i, I1 = 0, I2 = 0;
            int[,] sort_bound1;
            int[,] sort_bound2;

            sort_bound1 = new int[bi + 1, 2];
            sort_bound2 = new int[bi + 1, 2];

            bool hand1_found = false;
            int hand_num = 0;

            sort_bound1[I1, 0] = sort_bound[0, 0];
            sort_bound1[I1, 1] = sort_bound[0, 1];
            I1++;
            for (i = 1; i < bi + 1; i++)//i started from 0 as 1st pixel also has j=329 but we need it
            {
                if (hand1_found == false)
                {

                    pixelData[sort_bound[i, 0] + sort_bound[i, 1] * DD] = 9000;

                    hand_total_x += sort_bound[i, 0];
                    hand_total_y += sort_bound[i, 1];

                    sort_bound1[I1, 0] = sort_bound[i, 0];
                    sort_bound1[I1, 1] = sort_bound[i, 1];
                    I1++;
                    if (sort_bound[i, 1] == 329)
                    {
                        hand1_found = true;
                        fi_1 = 0;
                        hand_num = 1;
                        if (I1 > 20)//newly added to remove flickering
                        {
                            X1 = hand_total_x / I1;
                            Y1 = hand_total_y / I1 + 20;
                            Canvas.SetLeft(palm, X1);
                            Canvas.SetTop(palm, Y1);
                        }
                        else
                        {
                            Canvas.SetLeft(palm, 0);
                            Canvas.SetTop(palm, 0);
                        }
                        hand_total_x = 0;
                        hand_total_y = 0;

                    }
                }

                if (hand1_found == true && sort_bound[i, 1] != 329)
                {
                    fi_2 = 0;
                    hand_total_x += sort_bound[i, 0];
                    hand_total_y += sort_bound[i, 1];

                    pixelData[sort_bound[i, 0] + sort_bound[i, 1] * DD] = 9000;
                    hand_num = 2;

                    sort_bound2[I2, 0] = sort_bound[i, 0];
                    sort_bound2[I2, 1] = sort_bound[i, 1];
                    I2++;
                }
            }

            if (I2 > 20)//newly added to remove flickering
            {
                X2 = hand_total_x / (I2 + 1);
                Y2 = hand_total_y / (I2 + 1) + 20;
                Canvas.SetLeft(palm2, X2);
                Canvas.SetTop(palm2, Y2);
            }
            else
            {
                Canvas.SetLeft(palm2, 0);
                Canvas.SetTop(palm2, 0);
            }

            int cnt = 1;
            hand_total_x = 0;
            hand_total_y = 0;
            if (hand_num == 1)
            {
                for (i = 180; i < 470; i++)//to cover whole image set i=0 & i<640
                {
                    for (int j = 160; j < 330; j++)//to cover whole image set j=0 & i<480
                    {

                        if (valid[i, j] == true)//for square use 1200
                        {
                            hand_total_x += i;
                            hand_total_y += j;
                            cnt++;
                        }
                    }
                }

                if (cnt > 20)//newly added to remove flickering
                {
                    X1 = hand_total_x / cnt;
                    Y1 = hand_total_y / cnt;
                    Canvas.SetLeft(palm, X1);
                    Canvas.SetTop(palm, Y1);
                }
                else
                {
                    Canvas.SetLeft(palm, 0);
                    Canvas.SetTop(palm, 0);
                }
            }

            //done due to error pixels
            if (I1 > 20)
            {
                K_algo(sort_bound1, 1, X1, Y1);
                K_algo(sort_bound2, 2, X2, Y2);
            }

            else
            {
                K_algo(sort_bound2, 1, X2, Y2);
            }
        }

        void K_algo(int[,] sort_bound, int hand, int X, int Y)
        {
            int k = 22, offset = 5;
            fingers = new int[5, 2];
            fi = -1;
            double x1, x2, y1, y2;
            int P = k;
            double alpha;

            if (bi > 2 * k)
            {
                while (P + k <= bi)
                {
                    x1 = sort_bound[P, 0] - sort_bound[P - k, 0];
                    y1 = sort_bound[P, 1] - sort_bound[P - k, 1];

                    x2 = sort_bound[P, 0] - sort_bound[P + k, 0];
                    y2 = sort_bound[P, 1] - sort_bound[P + k, 1];

                    double sqr = Math.Sqrt((x1 * x1) + (y1 * y1)) * Math.Sqrt((x2 * x2) + (y2 * y2));

                    double cosAlpha = (x1 * x2 + y1 * y2) / sqr;
                    alpha = Math.Acos(cosAlpha) * 180 / Math.PI;

                    if (alpha < 45)
                    {
                        double d1, d2;
                        d1 = Math.Sqrt(Math.Pow(((sort_bound[P + k, 0] + sort_bound[P - k, 0]) / 2 - X), 2) + Math.Pow(((sort_bound[P + k, 1] + sort_bound[P - k, 1]) / 2 - Y), 2));
                        d2 = Math.Sqrt(Math.Pow((sort_bound[P, 0] - X), 2) + Math.Pow((sort_bound[P, 1] - Y), 2));

                        if (d2 > d1)//if we remove this condition then we can also get points between two fingers
                        {
                            fi++;
                            fingers[fi, 0] = sort_bound[P + offset, 0];
                            fingers[fi, 1] = sort_bound[P + offset, 1];
                            P += 21;
                            P += offset;
                        }
                    }

                    if (fi == 4)// newly added
                        break;
                    P++;
                }
            }

            //   Console.WriteLine("fi=" + fi + " bi=" + bi + " hand=" + hand_cnt);

            ////imp part for checking
            /*   if (bi > 110)
               {
                   Canvas.SetLeft(tip1, sort_bound[110, 0]);
                   Canvas.SetTop(tip1, sort_bound[110, 1]);
                   x1 = sort_bound[88, 0] - sort_bound[66, 0];
                   y1 = sort_bound[88, 1] - sort_bound[66, 1];
               

                   x2 = sort_bound[88, 0] - sort_bound[110, 0];
                   y2 = sort_bound[88, 1] - sort_bound[110, 1];
               
                     double sqr = Math.Sqrt((x1 * x1) + (y1 * y1)) * Math.Sqrt((x2 * x2) + (y2 * y2));

                     double cosAlpha = (x1 * x2 + y1 * y2) / sqr;
                     alpha = Math.Acos(cosAlpha) * 180 / Math.PI;


                   //  Console.WriteLine(" " + alpha + " " + cosAlpha + " " + bi + " " + sort_bound[88, 0] + " " + sort_bound[66, 0] + " " + X + " " + Y);
               }
               if (bi > 22)
               {
                   Canvas.SetLeft(tip2, sort_bound[22, 0]);
                   Canvas.SetTop(tip2, sort_bound[22, 1]);
               }
               if (bi > 44)
               {
                   Canvas.SetLeft(tip3, sort_bound[44, 0]);
                   Canvas.SetTop(tip3, sort_bound[44, 1]);
               }    
               if (bi > 66)
               {
                   Canvas.SetLeft(tip4, sort_bound[66, 0]);
                   Canvas.SetTop(tip4, sort_bound[66, 1]);
               }
               if (bi > 88)
               {
                   Canvas.SetLeft(tip5, sort_bound[88, 0]);
                   Canvas.SetTop(tip5, sort_bound[88, 1]);
               }
            

            //*/
            finger_tip(hand);

        }

        void finger_tip(int hand)
        {
            if (hand == 1)
            {
                if (fi >= 0)
                {
                    Canvas.SetLeft(tip1, fingers[0, 0]);
                    Canvas.SetTop(tip1, fingers[0, 1]);
                    fi_1 = 1;
                }
                else
                {
                    Canvas.SetLeft(tip1, 0);
                    Canvas.SetTop(tip1, 0);
                }

                if (fi >= 1)
                {
                    Canvas.SetLeft(tip2, fingers[1, 0]);
                    Canvas.SetTop(tip2, fingers[1, 1]);
                    fi_1 = 2;
                    l = new Line();
                    l.X1 = fingers[0, 0];
                    l.X2 = fingers[1, 0];
                    l.Y1 = fingers[0, 1];
                    l.Y2 = fingers[1, 1];
                    l.Stroke = System.Windows.Media.Brushes.Green;
                    l.StrokeThickness = 1;
                }
                else
                {
                    Canvas.SetLeft(tip2, 0);
                    Canvas.SetTop(tip2, 0);
                }

                if (fi >= 2)
                {
                    Canvas.SetLeft(tip3, fingers[2, 0]);
                    Canvas.SetTop(tip3, fingers[2, 1]);
                    fi_1 = 3;
                  
                    Line l2 = new Line();
                    l2.X1 = fingers[1, 0];
                    l2.X2 = fingers[2, 0];
                    l2.Y1 = fingers[1, 1];
                    l2.Y2 = fingers[2, 1];
                    l2.Stroke = System.Windows.Media.Brushes.Green;
                    l2.StrokeThickness = 1;
                  
                }
                else
                {
                    Canvas.SetLeft(tip3, 0);
                    Canvas.SetTop(tip3, 0);
                }

                if (fi >= 3)
                {
                    Canvas.SetLeft(tip4, fingers[3, 0]);
                    Canvas.SetTop(tip4, fingers[3, 1]);
                    fi_1 = 4;
                    Line l3 = new Line();
                    l3.X1 = fingers[2, 0];
                    l3.X2 = fingers[3, 0];
                    l3.Y1 = fingers[2, 1];
                    l3.Y2 = fingers[3, 1];
                    l3.Stroke = System.Windows.Media.Brushes.Green;
                    l3.StrokeThickness = 1;
                }
                else
                {
                    Canvas.SetLeft(tip4, 0);
                    Canvas.SetTop(tip4, 0);
                }

                if (fi >= 4)
                {
                    Canvas.SetLeft(tip5, fingers[4, 0]);
                    Canvas.SetTop(tip5, fingers[4, 1]);
                    fi_1 = 5;
                    Line l4 = new Line();
                    l4.X1 = fingers[3, 0];
                    l4.X2 = fingers[4, 0];
                    l4.Y1 = fingers[3, 1];
                    l4.Y2 = fingers[4, 1];
                    l4.Stroke = System.Windows.Media.Brushes.Green;
                    l4.StrokeThickness = 1;
                   
                }
                else
                {
                    Canvas.SetLeft(tip5, 0);
                    Canvas.SetTop(tip5, 0);
                }
            }



            if (hand == 2)
            {

                if (fi >= 0)
                {
                    Canvas.SetLeft(tip6, fingers[0, 0]);
                    Canvas.SetTop(tip6, fingers[0, 1]);
                    fi_2 = 1;
                }
                else
                {
                    Canvas.SetLeft(tip6, 0);
                    Canvas.SetTop(tip6, 0);
                }

                if (fi >= 1)
                {
                    Canvas.SetLeft(tip7, fingers[1, 0]);
                    Canvas.SetTop(tip7, fingers[1, 1]);
                    fi_2 = 2;
                }
                else
                {
                    Canvas.SetLeft(tip7, 0);
                    Canvas.SetTop(tip7, 0);
                }

                if (fi >= 2)
                {
                    Canvas.SetLeft(tip8, fingers[2, 0]);
                    Canvas.SetTop(tip8, fingers[2, 1]);
                    fi_2 = 3;
                }
                else
                {
                    Canvas.SetLeft(tip8, 0);
                    Canvas.SetTop(tip8, 0);

                }

                if (fi >= 3)
                {
                    Canvas.SetLeft(tip9, fingers[3, 0]);
                    Canvas.SetTop(tip9, fingers[3, 1]);
                    fi_2 = 4;
                }
                else
                {
                    Canvas.SetLeft(tip9, 0);
                    Canvas.SetTop(tip9, 0);
                }

                if (fi >= 4)
                {
                    Canvas.SetLeft(tip10, fingers[4, 0]);
                    Canvas.SetTop(tip10, fingers[4, 1]);
                    fi_2 = 5;
                }
                else
                {
                    Canvas.SetLeft(tip10, 0);
                    Canvas.SetTop(tip10, 0);
                }
            }
        }

        void gesture(int f1, int f2)
        {
            if (frame_cnt == max_frm)
            {
                f1 = (int)Math.Round(f1_sum / max_frm);
                f2 = (int)Math.Round(f2_sum / max_frm);

                find_gest(f1, f2);

                if (f1 == -1)
                {
                    label1.Content = "hand_1_fingers: ~~";
                    label2.Content = "hand_2_fingers: ~~";
                }

                else if (f2 == -1)
                {
                    label1.Content = "hand_1_fingers: " + f1;
                    label2.Content = "hand_1_fingers: ~~";
                }

                else
                {
                    label1.Content = "hand_1_fingers: " + f1;
                    label2.Content = "hand_2_fingers: " + f2;
                }

            }

            else
            {
                f1_sum = f1_sum + f1;
                f2_sum = f2_sum + f2;
            }

        }

        void find_gest(int f1, int f2)
        {
            /*inc cnt according to gest
             * make all other cnt to 0
             * if cnt after inc is 3
             *      implement that gest And make that cnt=0
             * else do nothing
             */
            int G_num = -1;

            if (f1 == 0 && f2 == -1)
            {
                G_num = 0;
            }

            else if (f1 == 1 && f2 == -1)
            {
                G_num = 1;
            }

            else if (f1 == 2 && f2 == -1)
            {
                G_num = 2;
            }

            else if (f1 == 3 && f2 == -1)
            {
                G_num = 3;
            }

            else if (f1 == 4 && f2 == -1)
            {
                G_num = 4;
            }

            else if (f1 == 5 && f2 == -1)
            {
                G_num = 5;
            }

            else if (f1 == 0 && f2 == 0)
            {
                G_num = 6;
            }

            else if (f1 == 0 && f2 == 1)
            {
                G_num = 7;
            }

            else if (f1 == 0 && f2 == 2)
            {
                G_num = 8;
            }

            else if (f1 == 0 && f2 == 3)
            {
                G_num = 9;
            }

            else if (f1 == 0 && f2 == 4)
            {
                G_num = 10;
            }

            else if (f1 == 0 && f2 == 5)
            {
                G_num = 11;
            }

            else if (f1 == 5 && f2 == 1)
            {
                G_num = 12;
            }

            else if (f1 == 5 && f2 == 2)
            {
                G_num = 13;
            }

            else if (f1 == 5 && f2 == 3)
            {
                G_num = 14;
            }

            else if (f1 == 5 && f2 == 4)
            {
                G_num = 15;
            }

            else if (f1 == 5 && f2 == 5)
            {
                G_num = 16;
            }

            else if (f1 == 1 && f2 == 0)
            {
                G_num = 17;
            }

            else if (f1 == 1 && f2 == 1)
            {
                G_num = 18;
            }

            else if (f1 == 1 && f2 == 2)
            {
                G_num = 19;
            }

            else if (f1 == 1 && f2 == 3)
            {
                G_num = 20;
            }

            else if (f1 == 1 && f2 == 4)
            {
                G_num = 21;
            }

            else if (f1 == 1 && f2 == 5)
            {
                G_num = 22;
            }

            else if (f1 == 2 && f2 == 0)
            {
                G_num = 23;
            }

            else if (f1 == 2 && f2 == 1)
            {
                G_num = 24;
            }

            else if (f1 == 2 && f2 == 2)
            {
                G_num = 25;
            }

            else if (f1 == 2 && f2 == 3)
            {
                G_num = 26;
            }

            else if (f1 == 2 && f2 == 4)
            {
                G_num = 27;
            }

            else if (f1 == 2 && f2 == 5)
            {
                G_num = 28;
            }

            for (int i = 0; i < 29; i++)
            {
                if (i == G_num)
                {
                      button_cnt[i]++;
                     if (button_cnt[i] == 3)//set it to 5 and see 
                    {
                        label3.Content = "GESTURE:-" + G_num;//code to send data to arduino
                        send_to_arduino(G_num);
                        button_cnt[i] = 0;
                    }
                }
                else
                {
                    button_cnt[i] = 0;
                }
            }

        }

        void send_to_arduino(int gest)
        {

            serialPort1.Open();
            if (serialPort1.IsOpen)
            {
                serialPort1.Write(remote[gest]);
            }
            serialPort1.Close();
        }

        /****************************************My Function****************************************/
        /*-----------------------------------------------------------------------------------------*/



        private float ScaleVector(int length, float position)
        {
            float value = (((((float)length) / 1f) / 2f) * position) + (length / 2);
            if (value > length)
            {
                return (float)length;
            }
            if (value < 0f)
            {
                return 0f;
            }
            return value;
        }

        void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            sensor.Stop();

            serialPort1.Open();
            if (serialPort1.IsOpen)
            {
                serialPort1.Write("s");
            }
            serialPort1.Close();
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //RuntimeOptions.UseDepth is used because I am obtaining depth data
            sensor.Start();
        }

    }
}

