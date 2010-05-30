/*
 * Version:
 * $Id: HeaderInfo.cs,v 1.13 2007/02/22 02:32:03 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: HeaderInfo.cs,v $
 * Revision 1.13  2007/02/22 02:32:03  kjb9089
 * - Final Version before final submission
 *
 * Revision 1.12  2007/02/07 04:26:38  kjb9089
 * - Fixed as many fricken errors as I could. Soooo frustrating.
 *
 * Revision 1.11  2007/02/06 18:08:11  kjb9089
 * - Fixed file upload-related issues
 *
 * Revision 1.10  2007/02/05 05:54:47  kjb9089
 * - Lots and lots of growling.
 *
 * Revision 1.9  2007/02/04 20:05:46  kjb9089
 * - Refined the way received data is handled. It is now more efficient.
 * - Changed the way data is parsed, uses more regex instead of substrings now
 *
 * Revision 1.8  2007/02/03 22:16:32  kjb9089
 * - Corrected errors related to form input
 * - Added support for file upload and thus avatars
 * - Added more error codes
 *
 * Revision 1.7  2007/02/02 15:36:08  kjb9089
 * - Start of adding better multipart forms (will lead to the ability to upload files)
 *
 * Revision 1.6  2007/01/25 17:27:00  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.5  2007/01/22 18:44:36  kjb9089
 * - Added replace functionality to AddValue method
 *
 * Revision 1.4  2007/01/22 03:14:47  kjb9089
 * - Added index search feature (class[key]=value)
 *
 * Revision 1.3  2007/01/13 18:36:17  kjb9089
 * - Attempted to add Opera support
 *
 * Revision 1.2  2007/01/12 15:24:52  kjb9089
 * - Expanded functionality so that HeaderInfo variables can be added upon after
 *   being constructed.
 *
 * Revision 1.1  2007/01/11 19:03:15  kjb9089
 * - Class file to assist the WebServer class
 *
 */

using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BattleGrounds {
   /// <summary>
   /// A class designed to hold and maintain header information
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class HeaderInfo {
      private Hashtable infoTable = new Hashtable();
      internal Hashtable InfoTable { get { return infoTable; } }

      private string[] theLines;
      private string method = "", fullPath = "", fileName = "", httpVer = "";
      internal string Method { get { return method; } }
      internal string FullPath { get { return fullPath; } set { fullPath = value; } }
      internal string FileName { get { return fileName; } set { fileName = value; } }
      internal string HttpVer { get { return httpVer; } }

      private int contentLength = 0;
      private int requestCount = 0;
      internal int ContentLength { get { return contentLength; } }
      internal int RequestCount { get { return requestCount; } set { requestCount++; } }

      internal FormData parsedData = null;
      internal FormData cookieData = null;
      internal Socket socket = null;
      internal List<byte> tmpFormData = new List<byte>();
      internal List<byte> tmpRecMsg = new List<byte>();
      internal byte[] receivedMsg = new byte[0];
      internal byte[] formData = new byte[0];
      internal byte[] returnData = new byte[0];
      internal string clientIP = "";
      internal string connection = "";
      internal string status = "";

      internal bool sendingErrMsg = false;
      internal bool msgSent = false;
      internal bool isLoggedIn = false;
      internal bool enqueued = false;
      internal bool isReady = false;

      internal DateTime startTime;

      /// <summary>
      /// Basic constructor. Does nothing.
      /// </summary>
      public HeaderInfo() { startTime = DateTime.Now; }

      /// <summary>
      /// Adds supplied name/value pair to the infoTable
      /// </summary>
      /// <param name="name">Name to add</param>
      /// <param name="value">Value associated with given name</param>
      internal void AddValue(string name, string value) {
         if (!infoTable.ContainsKey(name)) {
            infoTable.Add(name, value);
         } else {
            infoTable[name] = value;
         }
      }

      /// <summary>
      /// Returns the value of the given index
      /// or an empty string if not found.
      /// </summary>
      /// <param name="name">Index to search for</param>
      /// <returns>Value associated with given index.</returns>
      internal string GetValue(string name) {
         if (infoTable.ContainsKey(name)) {
            return "" + infoTable[name];
         }
         return "";
      }

      /// <summary>
      /// Handles POST data after all data has been
      /// retrieved.
      /// </summary>
      internal void HandlePOSTRec() {
         // tmpRecMsg.AddRange(receivedMsg);
         tmpFormData.RemoveRange(ContentLength, tmpFormData.Count - ContentLength);
         formData = tmpFormData.ToArray();
         tmpRecMsg.AddRange(Encoding.ASCII.GetBytes("\r\n\r\n"));
         tmpRecMsg.AddRange(formData);
         receivedMsg = tmpRecMsg.ToArray();
         POSTData.HandlePostMsg(this);
         PrepareToSend();
      }

      /// <summary>
      /// Sets the contents of the infoTable using the given
      /// Header buffer.
      /// </summary>
      /// <param name="theBuffer"></param>
      internal void ParseHeader(byte[] recMsg) {
         List<byte> tmpRecMsg = new List<byte>(recMsg);
         string theBuffer = Encoding.ASCII.GetString(recMsg);
         theBuffer = Regex.Replace(theBuffer, "\0", "");
         if (theBuffer.Contains("\r\n\r\n") && (theBuffer.Substring(0, 
             theBuffer.IndexOf("\r\n")).Contains("POST") || method == "POST")) {
            string tmpBuf = theBuffer.Substring(0, theBuffer.IndexOf("\r\n\r\n"));
            formData = new byte[recMsg.Length - tmpBuf.Length - 4];
            tmpRecMsg.CopyTo(tmpBuf.Length + 4, formData, 0, formData.Length);
            tmpRecMsg.RemoveRange(tmpBuf.Length, formData.Length + 4);
            recMsg = tmpRecMsg.ToArray();
         } else if (method == "POST") {
            formData = recMsg;
            theBuffer = "";
         }
         theBuffer = Encoding.ASCII.GetString(recMsg);
         theLines = Regex.Split(theBuffer, "\r\n");

         if (theLines[0].Contains("HTTP/")) {
            method = theLines[0].Split(' ')[0];
            fullPath = theLines[0].Substring(method.Length,
               theLines[0].IndexOf("HTTP") - method.Length).Trim();
            if (!fullPath.Contains(".") && fullPath.Length > 0 && 
                fullPath[fullPath.Length - 1] != '/') {
               fullPath += "/";
            }
            if (fullPath.Length > 0 && fullPath[0] == '/') {
               fullPath = fullPath.Substring(1);
            }

            if (WebServer.isBGServer(clientIP) && fullPath.Contains(".xml")) {
               fullPath = WebServer.UserDir + fullPath;
            } else if (fullPath.Contains("avatars/")) {
               fullPath = WebServer.UserDir + fullPath;
               if (!File.Exists(fullPath)) {
                  fullPath = WebServer.UserDir + "avatars/bg.jpg";
               }
            } else {
               fullPath = WebServer.WorkingDir + fullPath;
            }

            fileName = fullPath.Substring(fullPath.LastIndexOf('/'));
            if (fileName.Equals("/") || fileName.Equals("")) {
               fileName = WebConnection.GetDefaultFileName(fullPath);
               fullPath += fileName;
            }
            httpVer = theBuffer.Substring(theBuffer.IndexOf("HTTP"), 8);
         }

         Console.WriteLine("Preparing File: " + fileName);

         for (int i = 0; i < theLines.Length; i++) {
            if (!theLines[i].Contains(method) && !theLines[i].Contains(httpVer)) {
               string[] contents = Regex.Split(theLines[i], ": ");
               if (contents.Length == 2) {
                  AddValue(contents[0], contents[1]);
               }
            }
         }

         if (this["Connection"] != "") {
            connection = this["Connection"].ToLower();
         }

         if (this["Content-Type"] != "") {
            string[] contentType = Regex.Split(GetValue("Content-Type"), "boundary=");
            infoTable["Content-Type"] = contentType[0];
            if (contentType.Length == 2) {
               AddValue("boundary", contentType[1]);
            }
            if (this["Content-Length"] != "") {
               contentLength = int.Parse(GetValue("Content-Length"));
            }
         }

         receivedMsg = recMsg;
         tmpRecMsg.AddRange(recMsg);
         if (method == "POST") {
            if (contentLength > 0) {
               if (contentLength < WebServer.maxFileUploadSize) {
                  int newSize = ContentLength - formData.Length;
                  tmpFormData.AddRange(formData);
                  if (newSize > 0) {
                     WebConnection.recMsgQueue.Enqueue(this);
                  } else {
                     HandlePOSTRec();
                  }
               } else {

                  throw new BGException("451", new string[] { 
                     "" + ((int)(WebServer.maxFileUploadSize / 1024) + "kb") });
               }
            } else {
               WebConnection.keepAliveQueue.Enqueue(new KeepAlive(this));
            }
         } else if (method == "GET") {
            isReady = true;
            GETData.HandleGetMsg(this);
         }
      }

      /// <summary>
      /// Prepares a received message for
      /// sending to the browser.
      /// </summary>
      internal void PrepareToSend() {
         msgSent = false;

         if (this["Cookie"] != "") {
            BGUsers.HandleCookie(this);
         }

         if (parsedData != null && parsedData.Length > 0) {
            isReady = WebConnection.HandleFormData(this);
         }

         if (BGUsers.CheckLogged(this)) {
            cookieData = BGUsers.GetUserData(cookieData["nick"]);
         }
         WebConnection.SendToClient(this);
      }

      /// <summary>
      /// Overrided ToString() method
      /// </summary>
      /// <returns>The Key/Value pairs of the infoTable</returns>
      public override string ToString() {
         string ret = "";
         foreach (string key in infoTable.Keys) {
            ret += key + ": " + infoTable[key] + "\r\n";
         }
         return ret;
      }

      internal string this[string key] {
         get {
            return GetValue(key);
         }
         set {
            AddValue(key, value);
         }
      }
   }
}