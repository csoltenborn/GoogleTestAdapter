The msdia* files are taken from ``C:\Program Files (x86)\Microsoft Visual Studio {11,12,14}.0\DIA SDK\{include,idl}``.

dia2_merged.idl is a merge of those in order to make them usable in one typelib.
If we use the three original typelibs, common types like IDiaSymbol will be present
as three incompatible versions although in fact they are the same.