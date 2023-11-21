// Copyright (c) Microsoft. All rights reserved.

namespace Plugins;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SemanticKernel;


public sealed class MenuPlugin
{
    [SKFunction] [Description("Provides a list of specials from the menu.")]
    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Too smart")]
    public string GetSpecials() => @"
Special Soup: Clam Chowder
Special Salad: Cobb Chowder
Special Drink: Chai Tea
";


    [SKFunction] [Description("Provides the price of the requested menu item.")]
    public string GetItemPrice(
        [Description("The name of the menu item.")]
        string menuItem) => "$9.99";
}
