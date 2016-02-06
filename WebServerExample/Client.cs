using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;

namespace WebServerExample
{
    class Client
    {
        private string GetClientRequest(TcpClient Client)
        {
            string Request = "";
            byte[] Buffer = new byte[1024];
            int Count;

            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                if (Request.IndexOf("\r\n\r\n") >= 0 || Request.Length > 4096)
                {
                    break;
                }
            }

            return Request;
        }

        private void SendError(TcpClient Client, int Code)
        {
            string CodeStr = Code.ToString() + " " + ((HttpStatusCode)Code).ToString();
            string Html = "<html><body><h1>" + CodeStr + "!!!</h1></body></html>";
            string Str = "HTTP/1.1 " + CodeStr + "\nContent-type: text/html\nContent-Length:" + Html.Length.ToString() + "\n\n" + Html;
            byte[] Buffer = Encoding.ASCII.GetBytes(Str);
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
            Client.Close();
        }

        private string GetClientUri(TcpClient Client)
        {
            string Request = GetClientRequest(Client);

            Match ReqMatch = Regex.Match(Request, @"^\w+\s+([^\s\?]+)[^\s]*[^\W]\s+HTTP/.*|");

            if (ReqMatch == Match.Empty)
            {
                SendError(Client, 400);
                return null;
            }
            // Получаем строку запроса
            string RequestUri = ReqMatch.Groups[1].Value;
            RequestUri = Uri.UnescapeDataString(RequestUri);

            if (RequestUri.IndexOf("..") > 0)
            {
                SendError(Client, 400);
                return null;
            }

            if(RequestUri.EndsWith("/") == true)
            {
                RequestUri += "index.html";
            }

            return RequestUri;
        }

        private string GetContentType(string Extension)
        {
            // Тип содержимого
            string ContentType = "";

            switch (Extension)
            {
                case ".htm":
                case ".html":
                    ContentType = "text/html";
                    break;
                case ".css":
                    ContentType = "text/stylesheet";
                    break;
                case ".js":
                    ContentType = "text/javascript";
                    break;
                case ".jpg":
                    ContentType = "image/jpeg";
                    break;
                case ".jpeg":
                case ".png":
                case ".gif":
                    ContentType = "image/" + Extension.Substring(1);
                    break;
                default:
                    if (Extension.Length > 1)
                    {
                        ContentType = "application/" + Extension.Substring(1);
                    }
                    else
                    {
                        ContentType = "application/unknown";
                    }
                    break;
            }
            return ContentType;
        }

        public Client(TcpClient Client)
        {
            string RequestUri = GetClientUri(Client);
            if (RequestUri == null)
            {
                return;
            }

            string FilePath = "wwww" + RequestUri;
            if (!File.Exists(FilePath))
            {
                Console.Write(FilePath);
               SendError(Client, 404);
                return;
            }

            string Extension = RequestUri.Substring(RequestUri.LastIndexOf('.'));

            // Тип содержимого
            string ContentType = GetContentType(Extension);

            FileStream FS;
            try
            {
                FS = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            catch (Exception)
            {
                SendError(Client, 500);
                return;
            }
            // Посылаем заголовки
            string Headers = "HTTP/1.1 200 OK\nContent-Type: " + ContentType + "\nContent-Length: " + FS.Length + "\n\n";
            byte[] BufferHeaders = Encoding.ASCII.GetBytes(Headers);
            Client.GetStream().Write(BufferHeaders, 0, BufferHeaders.Length);

            int Count;
            byte [] Buffer = new byte[1024];
            // Пока не достигнут конец файла
            while (FS.Position < FS.Length)
            {
                // Читаем данные из файла
                Count = FS.Read(Buffer, 0, Buffer.Length);
                // И передаем их клиенту
                Client.GetStream().Write(Buffer, 0, Count);
            }

            // Закроем файл и соединение
            FS.Close();
            Client.Close();
        }
    }
}
