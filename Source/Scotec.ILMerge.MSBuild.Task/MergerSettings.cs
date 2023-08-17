#region MIT License

/*
    MIT License

    Copyright (c) 2016 Emerson Brito
    Copyright (c) 2023 Olaf Meyer

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
using System.Linq;
using System.Text.Json;

namespace Scotec.ILMerge.MsBuild.Task;

public class MergerSettings
{
    public GeneralSettings General { get; set; } = new();

    public AdvancedSettings Advanced { get; set; } = new();

    public string ToJson()
    {
        if (Advanced is { SearchDirectories: not null })
        {
            Advanced.SearchDirectories = Advanced.SearchDirectories.OrderBy(d => d).ToList();
        }

        var json = JsonSerializer.Serialize(this);
        return json;
    }

    public static MergerSettings FromJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            throw new ArgumentNullException(nameof(jsonString));
        }

        var obj = JsonSerializer.Deserialize<MergerSettings>(jsonString);

        return obj;
    }
}
