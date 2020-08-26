/* Copyright (c) rubicon IT GmbH
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;

namespace Core
{
  public class ReplacerError
  {
    public ReplacerError (LicenseHeaderInput input, bool calledByUser, ReplacerErrorType type, string description)
    {
      Input = input;
      CalledByUser = calledByUser;
      Type = type;
      Description = description;
    }

    public ReplacerError (LicenseHeaderInput input, ReplacerErrorType type, string description)
    {
      Input = input;
      CalledByUser = false;
      Type = type;
      Description = description;
    }

    public ReplacerErrorType Type { get; }

    public string Description { get; }

    /// <summary>
    /// Gets the <see cref="LicenseHeaderInput"/> instance the <see cref="LicenseHeaderReplacer"/> was invoked with.
    /// </summary>
    public LicenseHeaderInput Input { get; }

    public bool CalledByUser { get; }
  }
}