using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Internals.Tools.Ding.HighlightEngine;
using System.Windows.Media;

namespace GenerateHtml
{
    public class HtmlGenerator
    {
        public static void Generate(string inputFile, string outputFile)
        {
            FileInfo fileInfo = new FileInfo(inputFile);

            HighlightProfile profile = HighlightProfileFactory.GetProfileByExtension(fileInfo.Extension);
            string[] lines = File.ReadAllLines(inputFile);
            TextLineInfo[] infos = new HighlightParser(profile).Parse(lines, 0);

            StringBuilder sb = new StringBuilder();
            GenerateHtml(sb, infos);

            File.WriteAllText(new FileInfo(outputFile).FullName, sb.ToString());
        }

        private static void GenerateHtml(StringBuilder sb, TextLineInfo[] infos)
        {
            string header = @"
<html xmlns=""http://www.w3.org/1999/xhtml"" xml:lang=""en"" lang=""en"">
    <head>
        <style>
            <!--
.lineNumber {
    font-size:10.0pt;
    font-family:""Consolas"",""sans-serif"";
    background-color: Beige; 
    color: darkgray;
    width: 20pt;
}
.code {    
    font-size:10.0pt;
    font-family:""Consolas"",""sans-serif"";
    background-color: #ffffff; 
}
-->
        </style>
    </head>
    <body bgcolor=""white"" lang=""EN-US"" link=""blue"" vlink=""purple""  >
        <table class=""code"" style=""width:100%;cellpadding=""0""; cellspacing=""0"""">";

            sb.AppendLine(header);

            for (int i = 0; i < infos.Length; ++i)
            {
                FormatLine(sb, infos[i]);
            }

            string footer = @"
         </table>

    </body>
</html>";
            sb.AppendLine(footer);
        }

        private static void FormatLine(StringBuilder sb, TextLineInfo lineInfo)
        {
            int index = 0;
            string text;

            string lineHeader = @"<tr><td class=""lineNumber"">{0}</td><td>";

            sb.Append(string.Format(lineHeader, lineInfo.LineNumber));

            foreach (var seg in lineInfo.Segments)
            {
                if (seg.StartIndex != index)
                {
                    text = lineInfo.TextLine.Substring(index, seg.StartIndex - index);
                    sb.Append(FormatSegemnt(text, null));
                }

                text = lineInfo.TextLine.Substring(seg.StartIndex, seg.Length);
                sb.Append(FormatSegemnt(text, seg.Foreground));

                index = seg.StartIndex + seg.Length;
            }

            if (lineInfo.TextLine.Length != index)
            {
                text = lineInfo.TextLine.Substring(index, lineInfo.TextLine.Length - index);
                sb.Append(FormatSegemnt(text, null));
            }

            string lineTail = @"</td></tr>";
            sb.AppendLine(lineTail);
        }

        private static string FormatSegemnt(string text, Brush color)
        {
            text = text.Replace(" ", "&nbsp;");
            SolidColorBrush b = color as SolidColorBrush;
            return b == null ?
            text :
            string.Format("<span style=\"color:#{1:x2}{2:x2}{3:x2}\">{0}</span>", text, b.Color.R, b.Color.G, b.Color.B);
        }
    }
}
