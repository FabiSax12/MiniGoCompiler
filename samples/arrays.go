package main;

var arr [5]int;

func main() {
  var local_arr [3]int;

  arr[0] = 10;
  arr[1] = 20;
  arr[2] = 30;
  arr[3] = 40;
  arr[4] = 50;

  local_arr[0] = 100;
  local_arr[1] = 200;
  local_arr[2] = 300;

  println(arr[0]);
  println(arr[1]);
  println(arr[2]);
  println(arr[3]);
  println(arr[4]);

  println(len(arr));
  println(len(local_arr));

  var i int = 0;
  for i < len(local_arr) {
    println(local_arr[i]);
    i = i + 1;
  };
};
