/*
 * Version:
 * $Id: GETData.cs,v 1.3 2007/02/07 04:26:34 kjb9089 Exp $
 * 
 * Revisions:
 * $Log: GETData.cs,v $
 * Revision 1.3  2007/02/07 04:26:34  kjb9089
 * - Fixed as many fricken errors as I could. Soooo frustrating.
 *
 * Revision 1.2  2007/01/25 17:26:56  kjb9089
 * - Optimized code a bit (not perfectly, but better)
 * - Added admin rights (must edit .xml file to enable)
 * - Added more server-side parsing abilities
 *
 * Revision 1.1  2007/01/11 19:03:09  kjb9089
 * - Class file to assist the WebServer class
 *
 */

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace BattleGrounds {
   /// <summary>
   /// A class designed to handle GET messages
   /// received from the client.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   class GETData {
      /// <summary>
      /// A function used to handle GET messages
      /// </summary>
      /// <param name="httpVer">HTTP version</param>
      /// <param name="reqString">Requested File/Directory & input string</param>
      /// <param name="theSocket">Socket to utilize</param>
      private static FormData HandleFormGetMsg(string httpVer, string reqString) {
         string[] input = reqString.Substring(reqString.IndexOf('?') + 1).Split('&');
         FormData theData = new FormData();

         for (int i = 0; i < input.Length; i++) {
            theData.AddValue(input[i].Split('=')[0], input[i].Split('=')[1]);
         }

         return theData;
      }

      /// <summary>
      /// Handles the GET message type.
      /// </summary>
      /// <param name="theBuffer">Contents of the message.</param>
      /// <param name="theSocket">Socket to utilize</param>
      internal static void HandleGetMsg(HeaderInfo theInfo) {
         FormData theData = new FormData();
         string reqString = theInfo.FullPath;
         string reqFile = theInfo.FileName;

         if (reqString.IndexOf('?') > 0) {
            theData = HandleFormGetMsg(theInfo.HttpVer, reqString);
            reqString = reqString.Split('?')[0];
         }

         if (reqString.LastIndexOf(".") > 0) {
            reqFile = reqString.Substring(reqString.LastIndexOf('/') + 1);
         } else if (reqString.Substring(reqString.LastIndexOf('/')) != "") {
            reqString += "/";
         }

         if (reqFile == "") {
            reqFile = WebConnection.GetDefaultFileName(WebServer.WorkingDir + reqString);
            reqString += reqFile;
         }

         theInfo.FullPath = reqString;
         theInfo.FileName = reqFile;

         theInfo.parsedData = theData;
      }
   }
}
