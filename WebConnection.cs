/*
 * $Id: WebConnection.cs,v 1.1 2007/02/07 04:27:29 kjb9089 Exp $
 * 
 * $Log: WebConnection.cs,v $
 * Revision 1.1  2007/02/07 04:27:29  kjb9089
 * - Class to handle http connections.
 *
 * 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BattleGrounds {
   /// <summary>
   /// A class for handling web connections.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class WebConnection {
      private Thread keepAliveThread, newConnThread, sendMsgThread, recMsgThread;
      internal static Queue newConnQueue = new Queue();
      internal static Queue recMsgQueue = new Queue();
      internal static Queue keepAliveQueue = new Queue();
      private static Queue sendMsgQueue = new Queue();

      private static double ttlQueueCompleteTime = 0;
      internal static int ttlConnections = 0;

      private const int DEFAULT_REC_MSG_SIZE = 1024;
      private int recMsgSize = DEFAULT_REC_MSG_SIZE;

      /// <summary>
      /// Starts all the Connection threads.
      /// </summary>
      internal WebConnection() {
         keepAliveThread = new Thread(new ThreadStart(HandleKeepAlive));
         keepAliveThread.Start();

         newConnThread = new Thread(new ThreadStart(HandleNewConnections));
         newConnThread.Start();

         recMsgThread = new Thread(new ThreadStart(HandleRecMsgs));
         recMsgThread.Start();

         sendMsgThread = new Thread(new ThreadStart(HandleSendMsgs));
         sendMsgThread.Start();
      }

      /// <summary>
      /// Closes all threads
      /// </summary>
      internal void Close() {
         if (keepAliveThread != null) {
            keepAliveThread.Abort();
            keepAliveThread = null;
         }

         if (newConnThread != null) {
            newConnThread.Abort();
            newConnThread = null;
         }

         if (sendMsgThread != null) {
            sendMsgThread.Abort();
            sendMsgThread = null;
         }
      }

      /// <summary>
      /// Closes the specified socket.
      /// </summary>
      /// <param name="socket">Socket to close.</param>
      private void CloseConnection(Socket socket) {
         if (socket != null && socket.Connected) {
            Console.WriteLine("Closing Connection: " + socket.RemoteEndPoint);
            socket.Close();
            socket = null;
         }
      }

      /// <summary>
      /// Receives and handles data received from a client.
      /// </summary>
      /// <param name="theInfo">HeaderInfo variable to store received data in</param>
      /// <returns>byte[] array containing received information</returns>
      private void GetMessageContent(HeaderInfo theInfo) {
         if (theInfo.socket.Connected) {
            byte[] recMsg = new byte[recMsgSize];
            try {
               theInfo.socket.Receive(recMsg);
               theInfo.ParseHeader(recMsg);
            } catch (IOException) { }
            catch (SocketException) { }
         }
      }

      /// <summary>
      /// Returns the default file name to use
      /// when no file name is given
      /// </summary>
      /// <param name="thePath">The path to search in</param>
      /// <returns>The default file name</returns>
      internal static string GetDefaultFileName(string thePath) {
         for (int i = 0; i < WebServer.defaultFileNames.Length; i++) {
            if (File.Exists(thePath + WebServer.defaultFileNames[i])) {
               return WebServer.defaultFileNames[i];
            }
         }
         return "";
      }

      /// <summary>
      /// Returns the index for the specified error page.
      /// </summary>
      /// <param name="statusCode">Error page to look for</param>
      /// <returns>Requested index</returns>
      private static int GetDefaultStatusIndex(int statusCode) {
         for (int i = 0; i < WebServer.DEFAULT_ERR_CODES.Length; i++) {
            if (WebServer.DEFAULT_ERR_CODES[i].Split(' ')[0] == "" + statusCode) {
               return i;
            }
         }
         return 0;
      }

      /// <summary>
      /// Gets the content of the requested file and gathers information
      /// about it. If the file does not exist, it sends the necessary error msg.
      /// </summary>
      /// <param name="theInfo">Contains client information</param>
      /// <param name="theSocket">Socket to utilize to send error msg</param>
      /// <returns>A byte[] array containing the file contents</returns>
      internal static byte[] GetFileContents(string fullPath) {
         if (fullPath != "" && File.Exists(fullPath)) {
            byte[] theBytes = new byte[0];
            lock (FileLocker.GetLock(fullPath)) {
               FileStream theStream = new FileStream(fullPath, FileMode.Open,
                  FileAccess.Read, FileShare.Read);
               BinaryReader theReader = new BinaryReader(theStream);

               theBytes = new byte[theStream.Length];
               theReader.Read(theBytes, 0, theBytes.Length);

               theReader.Close();
               theStream.Close();
            }
            return theBytes;
         } else if (fullPath.Equals(WebServer.workingDir + "about//")) {
            return new byte[0];
         } else {
            if (fullPath.Contains(".")) {
               throw new BGException("404", new string[] { "File", fullPath });
            } else if (Directory.Exists(fullPath)) {
               throw new BGException("401", new string[0]);
            } else {
               throw new BGException("404", new string[] { "Directory", 
                  Regex.Replace(fullPath, "//", "/") });
            }
         }
      }


      /// <summary>
      /// Returns the MIME type for the requested
      /// file.
      /// </summary>
      /// <param name="theFile">The requested file</param>
      /// <returns>The MIME type of the requested file</returns>
      private string GetMIMEHeader(string theFile) {
         string ext = theFile.Substring(theFile.LastIndexOf(".") + 1);
         switch (ext) {
            case "bgw":
               goto case "html";
            case "bmp":
               return "image/bmp";
            case "css":
               return "text/css";
            case "gif":
               return "image/gif";
            case "htm":
               goto case "html";
            case "html":
               return "text/html";
            case "jpg":
               return "image/jpg";
            case "js":
               return "application/x-javascript";
            case "png":
               return "image/png";
            case "xml":
               return "application/xml";
            case "":
               goto case "html";
            default:
               return ext;
         }
      }

      /// <summary>
      /// Searches through the error codes for the
      /// requested status code.
      /// </summary>
      /// <param name="statusCode">Status code to search for</param>
      /// <returns>Index of requested status code</returns>
      private static int GetStatusIndex(int statusCode) {
         for (int i = 0; i < WebServer.errCodes.Length; i++) {
            if (WebServer.errCodes[i].Split(' ')[0] == "" + statusCode) {
               return i;
            }
         }
         return 0;
      }

      /// <summary>
      /// Handles both old and new client connections.
      /// </summary>
      /// <param name="theInfo">Variable containing header information</param>
      private void HandleConnections(HeaderInfo theInfo) {
         bool send = true;
         theInfo.enqueued = false;
         try {
            if (theInfo.socket != null && theInfo.socket.Connected) {
               theInfo.clientIP = theInfo.socket.RemoteEndPoint.ToString().Split(':')[0];

               ttlConnections++;
               GetMessageContent(theInfo);
            }

            if (theInfo.isReady && send) {
               theInfo.PrepareToSend();
            }
         } catch (BGException e) {
            SendErrMsg(int.Parse(e.Message), theInfo, e.msgs);
            send = false;
         }
      }

      /// <summary>
      /// Handles data that contains form information.
      /// </summary>
      /// <param name="theInfo">Variable containing the information.</param>
      internal static bool HandleFormData(HeaderInfo theInfo) {
         bool ret = true;
         if (theInfo.parsedData != null && theInfo.parsedData["action"] != "") {
            if (!theInfo.enqueued) {
               string date = "Sat, 22-Jan-2000 00:00:00 GMT";

               switch (theInfo.parsedData["action"]) {
                  case "adduser":
                     theInfo.parsedData = BGUsers.AddUser(theInfo.parsedData);
                     break;
                  case "delUser":
                     if (theInfo.parsedData["nick"] != null &&
                         theInfo.cookieData != null &&
                         theInfo.cookieData["rights"].Contains("admin")) {
                        if (BGUsers.DeleteUser(theInfo.parsedData["nick"])) {
                           theInfo.parsedData["MSG"] = "User '" + theInfo.parsedData["nick"] +
                              "' successfully deleted.";
                        } else {
                           theInfo.parsedData["MSG"] = "User '" + theInfo.parsedData["nick"] +
                              "' does not exist.";
                        }
                     } else {
                        ret = false;
                        throw new BGException("401", new string[0]);
                     }
                     break;
                  case "makeAdmin":
                     if (theInfo.cookieData != null &&
                         theInfo.cookieData["rights"].Contains("admin") &&
                         theInfo.parsedData["nick"] != null) {
                        BGUsers.MakeAdmin(theInfo.parsedData["nick"], true);
                     }
                     break;
                  case "updateUser":
                     if (theInfo.isLoggedIn) {
                        BGUsers.EditUser(theInfo);
                     } else {
                        ret = false;
                        throw new BGException("490", new string[0]);
                     }
                     break;
                  case "logIn":
                     Console.WriteLine("Logging In");
                     BGUsers.LogInUser(theInfo);
                     break;
                  case "logOut":
                     theInfo.AddValue("Set-Cookie", "username=; expires=" + date +
                        "|hash=; expires=" + date);
                     theInfo.AddValue("Cookie", "");
                     theInfo.isLoggedIn = false;
                     theInfo.cookieData = null;
                     break;
                  case "revokeAdmin":
                     if (theInfo.cookieData != null &&
                         theInfo.cookieData["rights"].Contains("admin") &&
                         theInfo.parsedData["nick"] != null) {
                        BGUsers.MakeAdmin(theInfo.parsedData["nick"], false);
                     }
                     break;
                  default:
                     ret = false;
                     throw new BGException("460", new string[] { theInfo.parsedData["action"] });
               }
            } else {
               ret = false;
            }
         } else {
            ret = false;
            throw new BGException("460", new string[0]);
         }
         return ret;
      }

      /// <summary>
      /// Loops through the keepAliveQueue, awaiting a connection made from the queue.
      /// Closes the connection if the keepAlive timeout has been reached.
      /// </summary>
      private void HandleKeepAlive() {
         KeepAlive current;
         Socket tmpSocket;
         while (true) {
            if (keepAliveQueue.Count > 0) {
               current = (KeepAlive)keepAliveQueue.Dequeue();
               tmpSocket = current.TheInfo.socket;
               if (DateTime.Now - current.StartTime >
                   TimeSpan.FromSeconds(WebServer.keepAlive) &&
                   !WebServer.isBGServer(current.TheInfo["Client-IP"])) {
                  if (tmpSocket != null) {
                     CloseConnection(tmpSocket);
                  }
               } else if (tmpSocket.Connected) {
                  if (tmpSocket.Available > 0) {
                     HeaderInfo theInfo = current.TheInfo;
                     theInfo.RequestCount++;
                     HandleConnections(theInfo);
                  } else {
                     keepAliveQueue.Enqueue(current);
                  }
               }
            }
            Thread.Sleep(10);
         }
      }


      /// <summary>
      /// Sets up a new client connection.
      /// </summary>
      private void HandleNewConnections() {
         Socket theSocket;
         while (true) {
            if (newConnQueue.Count > 0) {
               theSocket = (Socket)newConnQueue.Dequeue();
               if (theSocket.Connected) {
                  Console.WriteLine("Client Connected: " + theSocket.RemoteEndPoint);
                  HeaderInfo theInfo = new HeaderInfo();
                  theInfo.socket = theSocket;
                  HandleConnections(theInfo);
               }
            }
            Thread.Sleep(10);
         }
      }

      /// <summary>
      /// Handles the messages contained in the recMsgQueue.
      /// Loops through and retrieves data on regular intervals.
      /// This allows for multiple simultaneous connections.
      /// </summary>
      private void HandleRecMsgs() {
         while (true) {
            if (recMsgQueue.Count > 0) {
               HeaderInfo info = (HeaderInfo)recMsgQueue.Dequeue();
               if (info.socket.Connected) {
                  int newSize = info.ContentLength - info.tmpFormData.Count;
                  try {
                     if (newSize > 0) {
                        byte[] tmpMsg = new byte[(newSize > recMsgSize ? recMsgSize : newSize)];
                        info.socket.Receive(tmpMsg, 0, tmpMsg.Length, SocketFlags.None);
                        info.tmpFormData.AddRange(tmpMsg);
                        Console.WriteLine("Received " + (info.tmpFormData.Count) +
                           " out of " + info.ContentLength + " bytes from " +
                           info.socket.RemoteEndPoint);
                        if (newSize > tmpMsg.Length) {
                           recMsgQueue.Enqueue(info);
                        } else {
                           info.HandlePOSTRec();
                        }
                     } else {
                        info.HandlePOSTRec();
                     }
                  } catch (BGException e) {
                     SendErrMsg(int.Parse(e.Message), info, e.msgs);
                  } catch (SocketException) {
                     SendErrMsg(550, info, new string[0]);
                  }
               }
            }
            Thread.Sleep(10);
         }
      }

      /// <summary>
      /// When the message queue contains a message,
      /// HandleSendMsgs() sends it to the browser.
      /// </summary>
      private void HandleSendMsgs() {
         HeaderInfo theInfo;
         while (true) {
            if (sendMsgQueue.Count > 0) {
               theInfo = (HeaderInfo)sendMsgQueue.Dequeue();
               if (theInfo.FileName.Contains(".bgw") || theInfo.FullPath.Equals(WebServer.workingDir + "about//")) {
                  theInfo.returnData = BGWebParser.ParseFileContents(theInfo);
               }
               SendHeaderToBrowser(theInfo);
            }
            Thread.Sleep(10);
         }
      }



      /// <summary>
      /// Prepares the file and data for sending to the client.
      /// </summary>
      /// <param name="theInfo">Client Header Information</param>
      internal static void SendToClient(HeaderInfo theInfo) {
         try {
            if (!theInfo.sendingErrMsg && !theInfo.FullPath.Equals(WebServer.workingDir + "about//")) {
               theInfo.returnData = GetFileContents(theInfo.FullPath);
            }
            if (!theInfo.enqueued) {
               if (theInfo.sendingErrMsg || theInfo.isReady) {
                  theInfo.enqueued = true;
                  sendMsgQueue.Enqueue(theInfo);
               } else {
                  keepAliveQueue.Enqueue(new KeepAlive(theInfo));
               }
            }
         } catch (BGException e) {
            SendErrMsg(int.Parse(e.Message), theInfo, e.msgs);
         }
      }

      /// <summary>
      /// Send a header message to the browser.
      /// </summary>
      /// <param name="theInfo">Class containing all necessary information</param>
      private void SendHeaderToBrowser(HeaderInfo theInfo) {
         string theBuffer;
         string statusCode = theInfo.status;
         string MIME = GetMIMEHeader(theInfo.FileName);

         if (!File.Exists(theInfo.FullPath)) {
            MIME = "text/html";
            if (theInfo.FullPath.Contains(".xml") && WebServer.isBGServer(theInfo.clientIP)) {
               theInfo.returnData = new byte[0];
               statusCode = "404 File Not Found";
            }
         }

         theBuffer = theInfo.HttpVer + " " + (statusCode != "" ? statusCode : "200 OK") + "\r\n";
         theBuffer += "Server: " + WebServer.serverName + "\r\n";
         theBuffer += "Date: " + DateTime.Now.ToString() + "\r\n";
         if (theInfo["Last-Modified"] != "") {
            theBuffer += "Last-Modified: " + theInfo["Last-Modified"] + "\r\n";
         }
         theBuffer += "Accept-Ranges: bytes\r\n";
         theBuffer += "Connection: keep-alive\r\n";
         theBuffer += "Keep-Alive: timeout=" + WebServer.keepAlive + ",max=" +
            (WebServer.maxRequests - theInfo.RequestCount) + "\r\n";
         if (theInfo.returnData.Length != 0) {
            theBuffer += "Content-Type: " + MIME + "\r\n";
            theBuffer += "Content-Length: " + theInfo.returnData.Length + "\r\n";
         }

         if (theInfo["Set-Cookie"] != "") {
            string[] cookies = theInfo["Set-Cookie"].Split('|');
            for (int i = 0; i < cookies.Length; i++) {
               theBuffer += "Set-Cookie: " + cookies[i] + "\r\n";
            }
            theInfo["Set-Cookie"] = "";
         }
         theBuffer += "\r\n";

         List<byte> tmpList = new List<byte>(Encoding.ASCII.GetBytes(theBuffer));
         tmpList.AddRange(theInfo.returnData);

         if (theInfo.socket != null && theInfo.socket.Connected) {
            Console.WriteLine("Sending " + tmpList.Count + " bytes (" + theInfo.FileName +
               ") to " + theInfo.socket.RemoteEndPoint);
            SendMsgToBrowser(tmpList.ToArray(), theInfo.socket);

            Socket tmpSocket = theInfo.socket;
            bool sent = theInfo.enqueued;
            string conn = theInfo.connection;
            FormData parsedData = theInfo.parsedData;
            theInfo = new HeaderInfo();
            theInfo.connection = conn;
            theInfo.parsedData = parsedData;
            theInfo.socket = tmpSocket;
            theInfo.enqueued = sent;
            theInfo.sendingErrMsg = false;
         } else {
            theInfo.returnData = new byte[0];
         }

         theInfo.cookieData = null;
         if (theInfo.socket != null && theInfo.socket.Connected &&
             (theInfo.connection.ToLower().Contains("keep-alive") ||
              WebServer.isBGServer(theInfo.clientIP))) {

            Console.WriteLine("Enqueuing Connection: " + theInfo.socket.RemoteEndPoint);
            keepAliveQueue.Enqueue(new KeepAlive(theInfo));
         } else {
            CloseConnection(theInfo.socket);
         }
      }

      /// <summary>
      /// Send an Error message to the browser
      /// </summary>
      /// <param name="theStatus">Status code</param>
      /// <param name="theMsg">Class containing all necessary information.</param>
      /// <param name="extraInfo">Array containing information to replace in error message.</param>
      private static void SendErrMsg(int statusCode, HeaderInfo theInfo, string[] extraInfo) {
         int codeNum = GetStatusIndex(statusCode);
         string errCode = (codeNum >= 0 ? WebServer.errCodes[codeNum] :
            WebServer.DEFAULT_ERR_CODES[GetDefaultStatusIndex(statusCode)]);
         string errMsg = (codeNum >= 0 ? WebServer.errMsgs[codeNum] :
            WebServer.DEFAULT_ERR_MSGS[GetDefaultStatusIndex(statusCode)]); ;
         string errLayout = (codeNum >= 0 ? WebServer.errPageLayout :
            WebServer.DEFAULT_ERR_PAGE_LAYOUT);

         for (int i = 0; i < extraInfo.Length; i++) {
            errCode = Regex.Replace(errCode, @"\$" + i, extraInfo[i]);
            errMsg = Regex.Replace(errMsg, @"\$" + i, extraInfo[i]);
         }

         theInfo.status = errCode;

         errCode = errCode.Substring(errCode.IndexOf(' ') + 1);

         errLayout = Regex.Replace(errLayout, @"\$ERRCODE", errCode);
         errLayout = Regex.Replace(errLayout, @"\$ERRMSG", errMsg);

         theInfo.returnData = Encoding.ASCII.GetBytes(errLayout);
         theInfo.sendingErrMsg = true;

         if (theInfo["Cookie"] != "") {
            BGUsers.HandleCookie(theInfo);
         }

         Console.WriteLine("Sending Err Msg #" + statusCode + " to " +
            theInfo.socket.RemoteEndPoint);
         theInfo.returnData = BGWebParser.ParseFileContents(theInfo);
         SendToClient(theInfo);
      }

      /// <summary>
      /// Send a byte-based message to the browser
      /// </summary>
      /// <param name="data">Byte message to send</param>
      /// <param name="theSocket">Socket to utililze</param>
      private void SendMsgToBrowser(byte[] data, Socket theSocket) {
         try {
            if (theSocket.Connected) {
               theSocket.Send(data, data.Length, SocketFlags.None);
               Console.WriteLine(data.Length + " bytes sent.");
            }
         } catch (Exception e) { Console.WriteLine(e.Message); }
      }
   }

   /// <summary>
   /// Structure used to manage a persistent connection.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal struct KeepAlive {
      private HeaderInfo theInfo;
      internal HeaderInfo TheInfo {
         get { return theInfo; }
      }
      private DateTime startTime;
      internal DateTime StartTime {
         get { return startTime; }
      }

      internal KeepAlive(HeaderInfo theInfo) {
         this.theInfo = theInfo;
         startTime = DateTime.Now;
      }
   }
}