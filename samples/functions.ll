; ModuleID = 'functions'
source_filename = "functions"

@println_fmt = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.1 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.2 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@println_fmt.3 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

define i32 @add(i32 %0, i32 %1) {
entry:
  %a = alloca i32, align 4
  store i32 %0, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 %1, ptr %b, align 4
  %a1 = load i32, ptr %a, align 4
  %b2 = load i32, ptr %b, align 4
  %add = add i32 %a1, %b2
  ret i32 %add
}

define i32 @multiply(i32 %0, i32 %1) {
entry:
  %x = alloca i32, align 4
  store i32 %0, ptr %x, align 4
  %y = alloca i32, align 4
  store i32 %1, ptr %y, align 4
  %x1 = load i32, ptr %x, align 4
  %y2 = load i32, ptr %y, align 4
  %mul = mul i32 %x1, %y2
  ret i32 %mul
}

define void @printNumber(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %n1 = load i32, ptr %n, align 4
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt, i32 %n1)
  ret void
}

declare i32 @printf(ptr, ...)

define i32 @factorial(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %n1 = load i32, ptr %n, align 4
  %le = icmp sle i32 %n1, 1
  br i1 %le, label %if.then, label %if.merge

if.then:                                          ; preds = %entry
  ret i32 1

if.merge:                                         ; preds = %entry
  %n2 = load i32, ptr %n, align 4
  %n3 = load i32, ptr %n, align 4
  %sub = sub i32 %n3, 1
  %call = call i32 @factorial(i32 %sub)
  %mul = mul i32 %n2, %call
  ret i32 %mul
}

define void @main() {
entry:
  %call = call i32 @add(i32 5, i32 3)
  %result = alloca i32, align 4
  store i32 %call, ptr %result, align 4
  %result1 = load i32, ptr %result, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt.1, i32 %result1)
  %call2 = call i32 @multiply(i32 4, i32 7)
  %prod = alloca i32, align 4
  store i32 %call2, ptr %prod, align 4
  %prod3 = load i32, ptr %prod, align 4
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt.2, i32 %prod3)
  call void @printNumber(i32 42)
  %call4 = call i32 @factorial(i32 5)
  %fact5 = alloca i32, align 4
  store i32 %call4, ptr %fact5, align 4
  %fact55 = load i32, ptr %fact5, align 4
  %2 = call i32 (ptr, ...) @printf(ptr @println_fmt.3, i32 %fact55)
  ret void
}
