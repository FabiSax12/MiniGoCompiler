package main;

func main() {
  var numbers [10]int;
  var i int = 0;

  for i < 5 {
    numbers[i] = i * i;
    i = i + 1;
  };

  i = 0;
  for i < 5 {
    println(numbers[i]);
    i = i + 1;
  };

  var sum int = 0;
  i = 0;
  for i < 5 {
    sum = sum + numbers[i];
    i = i + 1;
  };

  println(`Sum:`);
  println(sum);
};
