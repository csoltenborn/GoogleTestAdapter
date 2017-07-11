#include "stdafx.h"
#include <iostream>

int main()
{
	std::cout << "Test output before crashing";
	int* pointer = 0;
    return *pointer;
}