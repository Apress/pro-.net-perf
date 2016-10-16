@echo off
nasm -f win32 vector_add_sse.asm -o vector_add_sse.obj
gcc -m32 vector_add_sse.obj -o vector_add_sse.exe
vector_add_sse.exe
echo [Return value was %errorlevel%]