public struct Constraints {
	public string[] front;
	public string[] right;
	public string[] back;
	public string[] left;


	public Constraints(string[] front, string[] right, string[] back, string[] left) {
		this.front = front;
		this.right = right;
		this.back = back;
		this.left = left;
	}


	public Constraints rotateCCW() {
		return new Constraints(left, front, right, back);
	}

	public Constraints rotateCW() {
		return new Constraints(right, back, left, front);
	}


	public Constraints invert() {
		string[] invertedFront = new string[front.Length];
		string[] invertedRight = new string[right.Length];
		string[] invertedBack = new string[back.Length];
		string[] invertedLeft = new string[left.Length];

		for(int index = 0; index < front.Length; index += 1) {
			invertedFront[invertedFront.Length - 1 - index] = front[index];
		}

		for(int index = 0; index < right.Length; index += 1) {
			invertedRight[invertedRight.Length - 1 - index] = right[index];
		}

		for(int index = 0; index < back.Length; index += 1) {
			invertedBack[invertedBack.Length - 1 - index] = back[index];
		}

		for(int index = 0; index < left.Length; index += 1) {
			invertedLeft[invertedLeft.Length - 1 - index] = left[index];
		}

		return new Constraints(invertedFront, invertedRight, invertedBack, invertedLeft);
	}


	public override bool Equals(object turd) {
		if(turd is Constraints that) {
			if(this.front.Length == that.front.Length && this.right.Length == that.right.Length && this.back.Length == that.back.Length && this.left.Length == that.left.Length) {
				for(int index = 0; index < front.Length; index += 1) {
					if(this.front[index] != that.front[index]) {
						return false;
					}
				}

				for(int index = 0; index < right.Length; index += 1) {
					if(this.right[index] != that.right[index]) {
						return false;
					}
				}

				for(int index = 0; index < back.Length; index += 1) {
					if(this.back[index] != that.back[index]) {
						return false;
					}
				}

				for(int index = 0; index < left.Length; index += 1) {
					if(this.left[index] != that.left[index]) {
						return false;
					}
				}

				return true;
			}
			else {
				return false;
			}
		}
		else {
			return false;
		}
	}

	public override int GetHashCode() {
		int code = 1;

		foreach(string turd in front) {
			code = 31 * code + turd.GetHashCode();
		}

		foreach(string turd in right) {
			code = 31 * code + turd.GetHashCode();
		}

		foreach(string turd in back) {
			code = 31 * code + turd.GetHashCode();
		}

		foreach(string turd in left) {
			code = 31 * code + turd.GetHashCode();
		}

		return code;
	}


	public override string ToString() {
		string turd = "";

		turd += string.Join("|", front);
		turd += " ";
		turd += string.Join("|", right);
		turd += " ";
		turd += string.Join("|", back);
		turd += " ";
		turd += string.Join("|", left);

		return turd;
	}
}
