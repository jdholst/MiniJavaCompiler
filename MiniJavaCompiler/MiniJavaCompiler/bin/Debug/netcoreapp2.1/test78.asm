.model small
.stack 100h
.data
.code
include io.asm
start PROC
mov ax, @data
mov ds, ax
call main
mov ah, 04ch
int 21h
start ENDP

 PROC
