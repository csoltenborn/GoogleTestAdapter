#include "stdafx.h"
#include <iostream>

#pragma warning(disable:6011)
int main()
{
	std::cout << "Test output before crashing";
	int* pointer = 0;
    return *pointer;
}