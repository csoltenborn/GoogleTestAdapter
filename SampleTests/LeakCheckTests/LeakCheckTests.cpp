#include "gtest/gtest.h"
#include <iostream>
#include <malloc.h>
#include <crtdbg.h>

// construct new singleton instance on first allocation, to perform memory leak reporting on destruction
// construction is performed in overridden 'new' operator (see Common\stdafx.cpp) to ensure its initialized
// before first allocation
namespace gsi
{
	namespace diag
	{
		class EnableLeakCheck
		{
		public:
			static void Initialize()
			{
				static EnableLeakCheck leakReporter;
			}
		private:
			EnableLeakCheck() {}
			~EnableLeakCheck()
			{
				_CrtDumpMemoryLeaks();
			}
		};
	}
}

void InitializeLeakCheck_()
{
	gsi::diag::EnableLeakCheck::Initialize();
}

#undef new

void* operator new(size_t size) //throw( std::bad_alloc )
{
	InitializeLeakCheck_();
	return malloc(size);
}

void* operator new[](size_t const size)
{
	InitializeLeakCheck_();
	return operator new(size);
}


bool is_run_by_gta = false;


namespace 
{
	template<typename T, size_t N> inline size_t lengthof(T const (&array)[N]) { return N; }
	template<int N> struct lengthof_sizer
	{
		unsigned char count[N];
	};
	template< class T, int N > inline lengthof_sizer<N> lengthof_get_sizer(T const (&array)[N]) { return lengthof_sizer<N>(); }
	#define LENGTHOF( a ) sizeof( lengthof_get_sizer( a ).count )

	class ShutdownReportHook
	{
	public:
		static void RegisterMemoryLeakAssertOnExit()
		{
			_CrtSetReportHookW2(_CRT_RPTHOOK_INSTALL, &RunHook_);
		}
	private:
		static int RunHook_
		(
			int   reportType,
			wchar_t *message,
			int  *returnValue
		)
		{
		   // Detect when the memory leak dump is starting, and flag for special processing of leak reports.
			const wchar_t szStartDumpString[] = L"Detected memory leaks!";	//CRT is hard coded to say "Detected memory leaks!\n" in the memory leak report
			if (::wcsncmp(message, szStartDumpString, LENGTHOF(szStartDumpString) - 1) == 0)
			{
            if (is_run_by_gta)
               std::cout << "GTA_RESULT_CODE_OUTPUT_BEGIN\n";
            std::wcout << std::wstring(message) << "\n";

			   exit(1);
			}

	      	return 0;
		}
	};
}

struct InitCleanupOnExit
{
	InitCleanupOnExit()
	{
		std::atexit(&CleanupOnExit_);	// Cleanup event sink on program shutdown.
	}

private:

	static void CleanupOnExit_()
	{
		// Memory leaks reports happen on shutdown after this point, so force them to go out via stderr
		// so any parent process will see it as an error.
		ShutdownReportHook::RegisterMemoryLeakAssertOnExit();
	}

} gInitCleanupOnExit;


int main(int argc, char** argv)
{
	std::string prefix("-is_run_by_gta");

   for (int i = 0; i < argc; i++)
	{
		if (strncmp(argv[i], prefix.c_str(), strlen(prefix.c_str())) == 0)
		{
         is_run_by_gta = true;
         break;
		}
	}

   testing::InitGoogleTest(&argc, argv);
	int returnValue = RUN_ALL_TESTS();

   #ifndef _DEBUG
   if (is_run_by_gta)
   {
      std::cout << "GTA_RESULT_CODE_SKIP\n";
      std::cout << "GTA_RESULT_CODE_OUTPUT_BEGIN\n";
      std::cout << "Memory leak detection is only performed if compiled with Debug configuration.\n";
   }
   #endif

   // memory leak detection will do exit(1) if memory leaks are found
   return is_run_by_gta ? 0 : returnValue;
}