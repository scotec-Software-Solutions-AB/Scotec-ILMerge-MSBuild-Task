﻿#region MIT License
/*
    MIT License

    Copyright (c) 2016 Emerson Brito

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace ILMerge.MsBuild.Task
{

    [DataContract]
    [KnownType(typeof(AdvancedSettings))]
    [KnownType(typeof(GeneralSettings))]
    public class MergerSettings
    {

        [DataMember(Order = 0)]
        public GeneralSettings General { get; set; } = new GeneralSettings();

        [DataMember(Order = 1)]
        public AdvancedSettings Advanced { get; set; } = new AdvancedSettings();

        public string ToJson()
        {

            string jsonString;

            using (MemoryStream stream = new MemoryStream())
            {

                var s = new DataContractJsonSerializerSettings()
                {
                    EmitTypeInformation = EmitTypeInformation.AsNeeded
                };

                var ds = new DataContractJsonSerializer(typeof(MergerSettings), s);

                ds.WriteObject(stream, this);
                jsonString = Encoding.UTF8.GetString(stream.ToArray());

            }

            return jsonString;

        }

        public static MergerSettings FromJson(string jsonString)
        {

            if (string.IsNullOrWhiteSpace(jsonString)) throw new ArgumentNullException(nameof(jsonString));

            MergerSettings results = null;

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString)))
            {
                var serializer = new DataContractJsonSerializer(typeof(MergerSettings));
                results = (MergerSettings) serializer.ReadObject(ms);
            }

            return results;

        }


    }
}