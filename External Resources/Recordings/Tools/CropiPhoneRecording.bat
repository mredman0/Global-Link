@echo off
setlocal enabledelayedexpansion

:: Check if the argument is provided
if "%~1"=="" (
    echo Usage: trim_video.bat "<path_to_video>"
    exit /b 1
)

:: Get the input file path and name
set "input_file=%~1"

:: Extract the directory, filename, and extension
for %%F in ("%input_file%") do (
    set "file_path=%%~dpF"
    set "file_name=%%~nF"
    set "file_ext=%%~xF"
)

:: Construct the output file path
set "output_file=%file_path%!file_name!_Cropped!file_ext!"

:: Run the FFmpeg command to crop the video
ffmpeg -i "%input_file%" -vf "crop=in_w:in_h-184:0:106" -c:a copy "%output_file%"

:: Check if the output file was successfully created
if exist "%output_file%" (
    echo Cropped video saved to: %output_file%
) else (
    echo Error occurred while processing the video.
)

endlocal