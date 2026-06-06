; ModuleID = 'comprehensive'
source_filename = "comprehensive"

@println_fmt = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1
@rawstr = private unnamed_addr constant [5 x i8] c"Sum:\00", align 1
@println_fmt.1 = private unnamed_addr constant [4 x i8] c"%s\0A\00", align 1
@println_fmt.2 = private unnamed_addr constant [4 x i8] c"%d\0A\00", align 1

define void @main() {
entry:
  %numbers = alloca [10 x i32], align 4
  store [10 x i32] zeroinitializer, ptr %numbers, align 4
  %i = alloca i32, align 4
  store i32 0, ptr %i, align 4
  br label %loop.cond

loop.cond:                                        ; preds = %loop.body, %entry
  %i1 = load i32, ptr %i, align 4
  %lt = icmp slt i32 %i1, 5
  br i1 %lt, label %loop.body, label %loop.exit

loop.body:                                        ; preds = %loop.cond
  %i2 = load i32, ptr %i, align 4
  %elem_ptr = getelementptr [10 x i32], ptr %numbers, i32 0, i32 %i2
  %i3 = load i32, ptr %i, align 4
  %i4 = load i32, ptr %i, align 4
  %mul = mul i32 %i3, %i4
  store i32 %mul, ptr %elem_ptr, align 4
  %i5 = load i32, ptr %i, align 4
  %add = add i32 %i5, 1
  store i32 %add, ptr %i, align 4
  br label %loop.cond

loop.exit:                                        ; preds = %loop.cond
  store i32 0, ptr %i, align 4
  br label %loop.cond6

loop.cond6:                                       ; preds = %loop.body7, %loop.exit
  %i9 = load i32, ptr %i, align 4
  %lt10 = icmp slt i32 %i9, 5
  br i1 %lt10, label %loop.body7, label %loop.exit8

loop.body7:                                       ; preds = %loop.cond6
  %i11 = load i32, ptr %i, align 4
  %elem_ptr12 = getelementptr [10 x i32], ptr %numbers, i32 0, i32 %i11
  %elem = load i32, ptr %elem_ptr12, align 4
  %0 = call i32 (ptr, ...) @printf(ptr @println_fmt, i32 %elem)
  %i13 = load i32, ptr %i, align 4
  %add14 = add i32 %i13, 1
  store i32 %add14, ptr %i, align 4
  br label %loop.cond6

loop.exit8:                                       ; preds = %loop.cond6
  %sum = alloca i32, align 4
  store i32 0, ptr %sum, align 4
  store i32 0, ptr %i, align 4
  br label %loop.cond15

loop.cond15:                                      ; preds = %loop.body16, %loop.exit8
  %i18 = load i32, ptr %i, align 4
  %lt19 = icmp slt i32 %i18, 5
  br i1 %lt19, label %loop.body16, label %loop.exit17

loop.body16:                                      ; preds = %loop.cond15
  %sum20 = load i32, ptr %sum, align 4
  %i21 = load i32, ptr %i, align 4
  %elem_ptr22 = getelementptr [10 x i32], ptr %numbers, i32 0, i32 %i21
  %elem23 = load i32, ptr %elem_ptr22, align 4
  %add24 = add i32 %sum20, %elem23
  store i32 %add24, ptr %sum, align 4
  %i25 = load i32, ptr %i, align 4
  %add26 = add i32 %i25, 1
  store i32 %add26, ptr %i, align 4
  br label %loop.cond15

loop.exit17:                                      ; preds = %loop.cond15
  %1 = call i32 (ptr, ...) @printf(ptr @println_fmt.1, ptr @rawstr)
  %sum27 = load i32, ptr %sum, align 4
  %2 = call i32 (ptr, ...) @printf(ptr @println_fmt.2, i32 %sum27)
  ret void
}

declare i32 @printf(ptr, ...)
