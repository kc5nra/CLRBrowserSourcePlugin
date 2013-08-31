// CLRBrowserSourcePipe.cpp : Defines the entry point for the console application.
//

#include <Windows.h>
#include <exception>
#using <mscorlib.dll>
#include <msclr\marshal_cppstd.h>

using namespace System;
using namespace System::Diagnostics;
using namespace System::IO;

using namespace msclr::interop;
using namespace Xilium::CefGlue;

std::wstring ToWString(System::String^ string)
{
    return marshal_as<std::wstring, System::String^>(string);
}



public ref class BrowserApp : public CefApp
{
public:
    virtual void OnRegisterCustomSchemes(CefSchemeRegistrar^ registrar) override
    {
        registrar->AddCustomScheme("local", true, true, false);
    }
};



int main(array<System::String ^> ^args)
{
    String^ currentDirectory = AppDomain::CurrentDomain->BaseDirectory;
    String^ libraryDirectory = Path::Combine(currentDirectory, L"CLRBrowserSourcePlugin");

    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"d3dcompiler_43.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"d3dcompiler_46.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"libGLESv2.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"libEGL.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"ffmpegsumo.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"icudt.dll")).c_str());
    LoadLibrary(ToWString(Path::Combine(libraryDirectory, L"libcef.dll")).c_str());
    
    try
    {
        try
        {
            Trace::WriteLine(String::Format("Starting browser proces with event args {0}", args));
            CefMainArgs ^mainArgs = gcnew CefMainArgs(args);
            return CefRuntime::ExecuteProcess(mainArgs, gcnew BrowserApp());
        }
        catch(const std::exception& stdEx)
        {
            throw gcnew System::Exception(gcnew System::String(stdEx.what()));
        }
    } 
    catch (Exception^ ex) 
    { 
        Trace::Fail(String::Format("Error while running browser child event loop: {0}", ex->Message));
        return 0;
    } 
    catch (...) 
    { 
        Trace::Fail("Error while running browser child event loop: {0}");
        return 0;
    }
        
    return 0;
}

int APIENTRY WinMain(HINSTANCE hInstance,
		     HINSTANCE hPrevInstance,
		     LPSTR lpCmdLine,
		     int nCmdShow)
{
    int argc;
    LPWSTR *argv;

    argv = CommandLineToArgvW(GetCommandLine(), &argc);

    array<String^>^ managedArgv = gcnew array<String^>(argc);
    for(int i = 0; i < argc; i++) {
        managedArgv[i] = gcnew String(argv[i]);
    }

    LocalFree(argv);

    return main(managedArgv);
}



