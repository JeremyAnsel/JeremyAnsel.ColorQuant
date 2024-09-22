# JeremyAnsel.ColorQuant

[![Build status](https://ci.appveyor.com/api/projects/status/u39upbktebxs5hwn/branch/master?svg=true)](https://ci.appveyor.com/project/JeremyAnsel/jeremyansel-colorquant/branch/master)
[![Code coverage](https://raw.githubusercontent.com/JeremyAnsel/JeremyAnsel.ColorQuant/gh-pages/coverage/badge_combined.svg)](https://jeremyansel.github.io/JeremyAnsel.ColorQuant/coverage/)
[![NuGet Version](https://img.shields.io/nuget/v/JeremyAnsel.ColorQuant)](https://www.nuget.org/packages/JeremyAnsel.ColorQuant)
![License](https://img.shields.io/github/license/JeremyAnsel/JeremyAnsel.ColorQuant)

JeremyAnsel.ColorQuant is a C# implementation of the Xiaolin Wu's Color Quantizer (v. 2).
For a given 32-bit RGB or ARGB image, it will produce a 8-bit palletized image.

Description     | Value
----------------|----------------
License         | [The MIT License (MIT)](https://github.com/JeremyAnsel/JeremyAnsel.ColorQuant/blob/master/LICENSE.txt)
Documentation   | http://jeremyansel.github.io/JeremyAnsel.ColorQuant
Code coverage   | https://jeremyansel.github.io/JeremyAnsel.ColorQuant/coverage/
Source code     | https://github.com/JeremyAnsel/JeremyAnsel.ColorQuant
Nuget           | https://www.nuget.org/packages/JeremyAnsel.ColorQuant
Build           | https://ci.appveyor.com/project/JeremyAnsel/jeremyansel-colorquant/branch/master

C Implementation of Xiaolin Wu's Color Quantizer (v. 2) (see Graphics Gems volume II, pages 126-133) : http://www.ece.mcmaster.ca/~xwu/cq.c.

> Algorithm: Greedy orthogonal bipartition of RGB space for variance minimization aided by inclusion-exclusion tricks. For speed no nearest neighbor search is done. Slightly better performance can be expected by more sophisticated but more expensive versions.

# Usage

For image with RGB data:
```csharp
var quantizer = new WuColorQuantizer();
ColorQuantizerResult result = quantizer.Quantize(image, colorCount);
```

For image with ARGB data:
```csharp
var quantizer = new WuAlphaColorQuantizer();
ColorQuantizerResult result = quantizer.Quantize(image, colorCount);
```

`ColorQuantizerResult` contains a palette and the result bytes.
