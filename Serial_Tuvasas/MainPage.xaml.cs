using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.SerialCommunication;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using System.Threading;
using Windows.UI.Core;

namespace Serial_Tuvasas
{
    public sealed partial class MainPage : Page
    {
        public string saat;
        public HttpClient client = new HttpClient();//Web apiden çekilmek için bir httpclient nesnesi üretiliyor
        private SerialDevice mySerial; // Seriden gönderilmek için değişken oluşturuluyor
        private Timer timer1; // zaman aralıklı göndermek için bir zaman değişkeni oluşturuluyor
        private Timer timer2;
        public DataWriter UART_Writer = null;//sadece yazmaya kullanıcağımız için bir datawriter değişkeni oluşturuluyor
        private string Elde;

        public MainPage()
        {
            this.InitializeComponent();
            String_al();//Httpget den string alınıyor
            SerialInit();//Aradaki seri bağlantının ayarları yapılıyor
            timer1 = new Timer(Zamanlayici1, null, (int)TimeSpan.FromSeconds(1).TotalMilliseconds, (int)TimeSpan.FromSeconds(1).TotalMilliseconds);
            timer2 = new Timer(Zamanlayici2, null, (int)TimeSpan.FromSeconds(1).TotalMilliseconds, (int)TimeSpan.FromMilliseconds(150).TotalMilliseconds);

            //zamanlayici fonksiyonuna zıplayacak triggeri 1 saniye sonra başlayan 1 saatte bir triggerlanan timer fonksiyonu oluşturuluyor

        }



        protected override void OnNavigatedTo(NavigationEventArgs e)//Program ilk açıldığında çalışıcaklar burda belirlenir
        {
            String_al();//Program ilk açıldığında tekrar httpget ile string çekiliyor
        }
        private void Manual_gonder(object sender, RoutedEventArgs e)//Tuşa basıldığında ne olacağı 
        {
            String_al();
            if (saati_al(saat) != null)
            {
                this.DuzString.Text = saati_al(saat);
                this.Tarih.Text = saat;
                Seriden_yaz(saati_al(saat));
            }
            else
            {
                this.DuzString.Text = "Service'a baglanilamiyor";
                String_al();
            }


        }
        public async void String_al()
        {
            try
            {
                var gelis = await client.GetStringAsync("http://193.1.1.5/saatapi/api/Saat");//Web serviceten bilgi çekiliyor
                saat = gelis.ToString();//stringe dönüştürülüyor
            }
            catch
            {
                String_al();
            }


        }
        private string saati_al(string Girdi)//aldığımız saat değeri seri porta uygun halde parçalanıp
                                             //birleştiriliyor
        {
            if (Girdi != null)
            {

                string str = "0";
                string str2 = Girdi;
                string str3 = str2.Substring(3, 2);
                string str4 = str2.Substring(6, 2);
                string str5 = str2.Substring(9, 2);
                string str6 = str2.Substring(12, 2);
                string str7 = str2.Substring(15, 2);
                string str8 = str2.Substring(18, 2);
                str = "07" + str5 + str4 + str3 + "1" + str6 + str7 + str8;

                return str;
            }
            else
            {
                return null;
            }
        }

        private void SerialInit()//Seri portun ayarlanma değerleri
        {
            Task.Run(async () =>//Task sınıfı Threadları kullandığından projemiz arka planda çalışmaya devam edebiliyor
            {
                string AQS = SerialDevice.GetDeviceSelector();//Device'a ait string alınıyor
                DeviceInformationCollection currentDevices = await DeviceInformation.FindAllAsync(AQS);//stringe ait bilgileri çekiliyor
                mySerial = await SerialDevice.FromIdAsync(currentDevices[0].Id);//biz bu cihazda UART0'ı kullandığımız için
                //0'ı seçiyoruz

                mySerial.BaudRate = 9600;//Baud rate ayarlanıyor
                mySerial.DataBits = 8;//8 bit olduğu
                mySerial.WriteTimeout = TimeSpan.FromMilliseconds(100);//zaman aşımı
                mySerial.StopBits = SerialStopBitCount.One;//durma biti
                mySerial.Parity = SerialParity.None;//eşlik biti


                UART_Writer = new DataWriter(mySerial.OutputStream);//serialimize ait outputstream datawriter değişkenimize eşitleniyor
                UART_Writer.UnicodeEncoding = UnicodeEncoding.Utf8;// utf8 kodlama kullanmasını söylüyoruz

            });
        }
        private async void Seriden_yaz(string Girdi)//Burası stringimizin yazıldığı yer
        {
            try// try catch blockları ile bir hata olsada program devam ediyor
            {
                if ((mySerial != null) & (UART_Writer != null) & (Girdi != ""))
                //if'ler yazılıyor
                {

                    UART_Writer.WriteString(Girdi);//yazma işlemi başlatılıyor
                    Task myFirstTaskAsync = Task.Factory.StartNew(async () =>// bir task oluşturup 
                                                                             //elimizdeki string dizisi bitene kadar beklenilmesi sağlanıyor
                    {
                        await UART_Writer.StoreAsync();
                    });
                    while (myFirstTaskAsync.IsCompleted == false) ;
                }
            }
            catch (Exception Ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>//bir hata oluşursa
                                                                            //hata yazılıyor
                {
                    Exception.Text = Ex.ToString();
                });
                UART_Writer = null;//nulla eşitlenip tekrar bir bağlantı kurulmaya çalışılıyor
                UART_Writer = new DataWriter(mySerial.OutputStream);
            }
        }
        public void Zamanlayici1(object sender)//state ifadesi kullanılmıyor sadece böyle yazılması için
        {//zamanlayıcı tetiklendikten sonra olacak işlemler
            String_al();//string al fonksiyu
            if (saati_al(saat) != null)
            {
                Elde = saati_al(saat);// yazdırma fonksiyonu

            }
        }
            public void Zamanlayici2(object sender)//state ifadesi kullanılmıyor sadece böyle yazılması için
            {//zamanlayıcı tetiklendikten sonra olacak işlemler
                if (Elde != null)
                {
                   Seriden_yaz(Elde);// yazdırma fonksiyonu

                }
            }
        



    }

    }

