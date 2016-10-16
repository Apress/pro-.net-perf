  extern _printf
  extern _malloc
  extern _free
  extern _getchar

  section .text
  global 	_main
  
_main:
  push	ebp
  mov	ebp, esp

  sub esp, 0xC
  
  push message
  call _printf
  add esp, 4  
  
  push dword 400
  call _malloc
  mov dword [ebp-4], eax
  push eax
  call _init_vector
  push dword 400
  call _malloc
  mov dword  [ebp-8], eax
  push eax
  call _init_vector
  push dword 400
  call _malloc
  mov dword  [ebp-0xC], eax
  
  push dword 100
  push dword [ebp-4]
  push dword [ebp-8]
  push dword [ebp-0xc]
  call _vector_add
  
  push dword [ebp-0xc]
  push print_fmt
  call _printf
  add esp, 8
  
  push end_message
  call _printf
  add esp, 4
  
  mov	esp, ebp
  pop	ebp
  ret
  
_vector_add:
  call _getchar
  mov ebx, dword  [esp+0x10]
  mov esi, dword  [esp+0xC]
  mov edi, dword  [esp+8]
  mov ecx, dword  [esp+4]
  xor edx, edx
NEXT_ITERATION:
  movups xmm1, [edi+edx*4]
  movups xmm0, [esi+edx*4]
  addps xmm1, xmm0
  movups [ecx+edx*4], xmm1
  add edx, 4
  cmp edx, ebx
  jg NEXT_ITERATION
  ret 0x10
  
_init_vector:
  cld
  mov edi, dword  [esp+4]
  mov eax, 0xc4142121
  mov ecx, 100
  rep stosd
  ret 4
	
  section		.data

  message     db 'Starting...',0xa,0xd,0
  print_fmt   db 'First element: %2.2f',0xa,0xd,0
  end_message db 'Finished.',0xa,0xd,0