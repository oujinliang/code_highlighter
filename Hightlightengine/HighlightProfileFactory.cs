/* Copyright (C) 2012  Jinliang Ou */

namespace Org.Jinou.HighlightEngine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows.Media;
    using System.Xml.Linq;

    public static class HighlightProfileFactory
    {
        private static IDictionary<string, string> Mapping = LoadMapping();
        private static IDictionary<string, HighlightProfile> Profiles = new Dictionary<string, HighlightProfile>();

        /// <summary>
        /// Get the highlight profile by file extension.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static HighlightProfile GetProfileByExtension(string extension)
        {
            string profileName;
            if (!Mapping.TryGetValue(extension, out profileName))
            {
                return null;
            }

            return GetProfileByName(profileName);
        }

        /// <summary>
        /// Get profile by name.
        /// </summary>
        /// <param name="profileName"></param>
        /// <returns></returns>
        public static HighlightProfile GetProfileByName(string profileName)
        {
            HighlightProfile highlightProfile;
            if (!Profiles.TryGetValue(profileName, out highlightProfile))
            {
                Profiles[profileName] = highlightProfile = LoadProfile(profileName);
            }

            return highlightProfile;
        }

        #region Private methods

        private static HighlightProfile LoadProfile(string key)
        {
            XElement xe = XElement.Parse(Resource.ResourceManager.GetString(key, Resource.Culture));
            bool ignoreCase = xe.Element("ignoreCase").NotNullValue(false);

            return new HighlightProfile
            {
                Delimiter = xe.Element("delimiter").NotNullValue(string.Empty).ToCharArray().Sort(),
                BackDelimiter = xe.Element("backDelimiter").NotNullValue(string.Empty).ToCharArray().Sort(),
                IgnoreCase = ignoreCase,

                KeywordCollecions = xe.Elements("keywords").ConvertToArray<KeywordCollection>(ConvertKeywords),
                MultiLinesBlocks = xe.Elements("multiLinesBlock").ConvertToArray<MultiLinesBlock>(ConvertMultiLinesBlock),
                SingleLineBlocks = xe.Elements("singleLineBlock").ConvertToArray<SingleLineBlock>(ConvertLineBlock),
                Tokens = xe.Elements("token").ConvertToArray<Token>(e => ConvertToken(e, ignoreCase)),
            };
        }

        private static KeywordCollection ConvertKeywords(XElement e)
        {
            return new KeywordCollection
            {
                Foreground = ConvertColor((string)e.Attribute("foreground")),
                Name = (string)e.Attribute("name"),
                Keywords = e.Elements("keyword").ConvertToArray(x => x.Value).Sort(),
            };
        }

        private static SingleLineBlock ConvertLineBlock(XElement e)
        {
            return ConvertBlock<SingleLineBlock>(e);
        }

        private static MultiLinesBlock ConvertMultiLinesBlock(XElement e)
        {
            return ConvertBlock<MultiLinesBlock>(e);
        }

        private static T ConvertBlock<T>(XElement e) where T : CodeBlock, new()
        {
            XElement escape = e.Element("escape");
            return new T
            {
                Name = (string)e.Attribute("name"),
                Start = e.Element("start").NotNullValue(string.Empty),
                End = e.Element("end").NotNullValue(string.Empty),

                Foreground = ConvertColor((string)e.Attribute("foreground")),
                WrapperForeground = ConvertColor((string)e.Attribute("wrapperForeground")),

                Escape = (escape == null) ? null : new BlockEscape
                {
                    EscapeString = (string)escape.Attribute("start"),
                    Items = escape.Elements("item").ConvertToArray(ee => ee.Value),
                },
            };
        }

        private static Token ConvertToken(XElement e, bool ignoreCase)
        {
            RegexOptions option = RegexOptions.Compiled | RegexOptions.Singleline;
            if (ignoreCase)
            {
                option |= RegexOptions.IgnoreCase;
            }

            return new Token
            {
                Name = (string)e.Attribute("name"),
                Foreground = ConvertColor((string)e.Attribute("foreground")),
                Pattern = new Regex("\\G" + (string)e.Attribute("pattern"), option),
                Groups = e.Elements("group").ConvertToArray(g =>
                    new TokenMatch
                    {
                        Name = (string)g.Attribute("name"),
                        Foreground = ConvertColor((string)g.Attribute("foreground")),
                    }),
            };
        }

        private static IDictionary<string, string> LoadMapping()
        {
            return XElement.Parse(Resource.mapping)
                .Elements("mapping")
                .ToDictionary(
                        e => e.Attribute("extension").Value,
                        e => e.Attribute("profile").Value,
                        StringComparer.InvariantCultureIgnoreCase);
        }

        private static Brush ConvertColor(string colorName)
        {
            BrushConverter converter = new BrushConverter();
            return string.IsNullOrEmpty(colorName) ? (Brush)null : (Brush)converter.ConvertFromString(colorName);
        }

        private static T[] ConvertToArray<T>(this IEnumerable<XElement> elements, Func<XElement, T> func)
        {
            return elements == null ? new T[0] : elements.Select(func).ToArray();
        }

        private static T NotNullValue<T>(this XElement e, T defaultValue)
        {
            return e == null ? defaultValue : (T)System.Convert.ChangeType(e.Value, typeof(T));
        }

        private static T[] Sort<T>(this T[] array)
        {
            Array.Sort(array);
            return array;
        }

        #endregion
    }
}
