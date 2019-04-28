#include "stdafx.h"

#include "ClrLibProject.h"

using namespace ClrDotNetLibProject;

int ClrLibProject::ClrClass::Add(int a, int b)
{
	DotNetClass^ instance = gcnew DotNetClass();
	return instance->Add(a, b);
}