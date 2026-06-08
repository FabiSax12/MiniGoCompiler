package main;

func add(a int, b int) int {
  return a + b;
};

func multiply(x int, y int) int {
  return x * y;
};

func printNumber(n int) {
  println(n);
};

func factorial(n int) int {
  if n <= 1 {
    return 1;
  };
  return n * factorial(n - 1);
};

func main() {
  var result int = add(5, 3);
  println(result);

  var prod int = multiply(4, 7);
  println(prod);

  printNumber(42);

  var fact5 int = factorial(5);
  println(fact5);
};
