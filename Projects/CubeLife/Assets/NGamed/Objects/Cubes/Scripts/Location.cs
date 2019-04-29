public struct Location {
	public int x;
	public int y;


	public Location(int x, int y) {
		this.x = x;
		this.y = y;
	}


	public override bool Equals(object turd) {
		if(turd is Location) {
			Location that = (Location) turd;

			return this.x == that.x && this.y == that.y;
		}
		else {
			return false;
		}
	}

	public override int GetHashCode() {
		return 31 * x + y;
	}
}
