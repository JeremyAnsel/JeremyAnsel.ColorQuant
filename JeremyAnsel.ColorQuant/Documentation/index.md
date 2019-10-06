# JeremyAnsel.ColorQuant

[![Build status](https://ci.appveyor.com/api/projects/status/u39upbktebxs5hwn/branch/master?svg=true)](https://ci.appveyor.com/project/JeremyAnsel/jeremyansel-colorquant/branch/master)
[![Code coverage](https://jeremyansel.github.io/JeremyAnsel.ColorQuant/coverage/badge_combined.svg)](https://jeremyansel.github.io/JeremyAnsel.ColorQuant/coverage/)
[![NuGet Version](https://buildstats.info/nuget/JeremyAnsel.ColorQuant)](https://www.nuget.org/packages/JeremyAnsel.ColorQuant)
![License](https://img.shields.io/github/license/JeremyAnsel/JeremyAnsel.ColorQuant)

JeremyAnsel.ColorQuant is a .Net color quantizer based on Xiaolin Wu's Color Quantizer. For a given 32-bit RGB or ARGB image, it will produce a 8-bit palletized image.

C Implementation of Xiaolin Wu's Color Quantizer (v. 2) (see Graphics Gems volume II, pages 126-133) : http://www.ece.mcmaster.ca/~xwu/cq.c. 

> Algorithm: Greedy orthogonal bipartition of RGB space for variance minimization aided by inclusion-exclusion tricks. For speed no nearest neighbor search is done. Slightly better performance can be expected by more sophisticated but more expensive versions. 
