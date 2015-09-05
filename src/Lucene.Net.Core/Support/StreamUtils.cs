﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public static class StreamUtils
    {
        static readonly BinaryFormatter Formatter = new BinaryFormatter();

        public static MemoryStream SerializeToStream(object o)
        {
            using (var stream = new MemoryStream())
            {
                Formatter.Serialize(stream, o);
                return stream;
            }
        }

        public static object DeserializeFromStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            object o = Formatter.Deserialize(stream);
            return o;
        }

        public static object DeserializeFromStream(BinaryReader reader)
        {
            var stream = reader.BaseStream;
            stream.Seek(0, SeekOrigin.Begin);
            object o = Formatter.Deserialize(stream);
            return o;
        }
    }
}
