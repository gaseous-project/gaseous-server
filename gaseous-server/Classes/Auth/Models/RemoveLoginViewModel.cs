// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Authentication;

public class RemoveLoginViewModel
{
    public string LoginProvider { get; set; }
    public string ProviderKey { get; set; }
}