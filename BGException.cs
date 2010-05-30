/*
 * $Id: BGException.cs,v 1.1 2007/02/22 02:32:31 kjb9089 Exp $
 * 
 * $Log: BGException.cs,v $
 * Revision 1.1  2007/02/22 02:32:31  kjb9089
 * - Final Version before final submission
 *
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace BattleGrounds {
   /// <summary>
   /// Exception class used for web server-related errors.
   /// <list type="bullet">
   /// <item>
   /// <term>Authors:</term>
   /// <description>Kenneth Beck</description>
   /// </item>
   /// </list>
   /// </summary>
   internal class BGException : Exception {
      public string[] msgs;
      public BGException(string statusCode, string[] msgs)
         : base(statusCode) {
         this.msgs = msgs;
      }
   }
}
