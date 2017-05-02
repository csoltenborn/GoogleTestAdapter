#pragma once

#ifdef DLL_LIBRARY_EXPORTS
#define DLL_LIBRARY_API __declspec(dllexport)   
#else  
#define DLL_LIBRARY_API __declspec(dllimport)   
#endif  

DLL_LIBRARY_API int ReturnZero();