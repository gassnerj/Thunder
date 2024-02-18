namespace MeteorologyCore;

public class Wind
{
    public Wind()
    {
    }

    public Wind(Direction? direction, double speed)
    {
        Direction = direction;
        Speed = speed;
    }

    public Direction? Direction { get; set; }
    public double Speed { get; set; }

}

public class Direction
{
    public string Cardinal
    {
        get
        {
            if (InRange(348.75, 360, this.Degrees))
            {
                return "N";
            }
            else if (InRange(11.25, 33.75, this.Degrees))
            {
                return "NNE";
            }
            else if (InRange(33.75, 56.25, this.Degrees))
            {
                return "NE";
            }
            else if (InRange(56.25, 78.75, this.Degrees))
            {
                return "ENE";
            }
            else if (InRange(78.75, 101.25, this.Degrees))
            {
                return "E";
            }
            else if (InRange(101.25, 123.75, this.Degrees))
            {
                return "ESE";
            }
            else if (InRange(123.75, 146.25, this.Degrees))
            {
                return "SE";
            }
            else if (InRange(146.25, 168.75, this.Degrees))
            {
                return "SSE";
            }
            else if (InRange(168.75, 191.25, this.Degrees))
            {
                return "S";
            }
            else if (InRange(191.25, 213.75, this.Degrees))
            {
                return "SSW";
            }
            else if (InRange(213.75, 236.25, this.Degrees))
            {
                return "SW";
            }
            else if (InRange(236.25, 258.75, this.Degrees))
            {
                return "WSW";
            }
            else if (InRange(258.75, 281.25, this.Degrees))
            {
                return "W";
            }
            else if (InRange(281.25, 303.75, this.Degrees))
            {
                return "WNW";
            }
            else if (InRange(303.75, 326.25, this.Degrees))
            {
                return "NW";
            }
            else if (InRange(326.25, 348.75, this.Degrees))
            {
                return "NNW";
            }
            else
            {
                return "N";
            }
        }
    }
    public double Degrees { get; private set; }

    public Direction(double d)
    {
        this.Degrees = d;
    }

    public override string ToString()
    {
        return this.Cardinal;
    }

    private bool InRange(double start, double end, double value)
    {
        return value >= start && value <= end ? true : false;
    }
}
