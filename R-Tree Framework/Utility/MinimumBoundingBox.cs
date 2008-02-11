using System;
using System.Collections.Generic;
using System.Text;
using R_Tree_Framework.Utility.Exceptions;
using System.Runtime.InteropServices;

namespace R_Tree_Framework.Utility
{
    /// <summary>
    /// This class represents an n-dimensional rectangle that serves as the minimum bounding rectangle
    /// of a spatial object.  For each dimension of the rectangle, a minimum value and a maximum value
    /// are stored.  Objects returned from this class that are modified will not affect the original
    /// object.  For example, changes made to the list returned from the 
    /// <see cref="MinimumBoundingBox.MinimumValues"/> property will not alter the minimum values
    /// stored within the MinimumBoundingBox object.  Alterations to the bounding box are not
    /// allowed after construction.
    /// </summary>
    /// <typeparam name="CoordinateType"></typeparam>
    public class MinimumBoundingBox<CoordinateType> : ISavable where CoordinateType : struct, IComparable
    {
        #region Instance Variables

        #region Protected

        protected Int32 dimension;
        protected List<CoordinateType> minimumValues, maximumValues;

        #endregion Protected

        #endregion Instance Variables
        #region Properties

        #region Public

        /// <summary>
        /// The number of dimensions in which the minimum bounding box exists.
        /// </summary>
        public virtual Int32 Dimension
        {
            get { return dimension; }
            protected set { dimension = value; }
        }
        /// <summary>
        /// A Pair of lists containing the minimum and maximum value in each dimension in which the minimum bounding box exists.
        /// </summary>
        public virtual Pair<List<CoordinateType>, List<CoordinateType>> Extremes
        {
            get { return new Pair<List<CoordinateType>, List<CoordinateType>>(MinimumValues, MaximumValues); }
        }
        /// <summary>
        /// A List of the maximum values for each dimension in which the minimum bounding box exists.
        /// </summary>
        public virtual List<CoordinateType> MaximumValues
        {
            get { return new List<CoordinateType>(maximumValues); }
            protected set { maximumValues = value; }
        }
        /// <summary>
        /// A List of the minimum values for each dimension in which the minimum bounding box exists.
        /// </summary>
        public virtual List<CoordinateType> MinimumValues
        {
            get { return new List<CoordinateType>(minimumValues); }
            protected set { minimumValues = value; }
        }

        #endregion Public

        #endregion Properties
        #region Constructors

        #region Public

        /// <summary>
        /// Constructs a minimum bounding box with the minimum values in each dimension as specified
        /// in the minimumValues parameter and with the corresponding maximum values as specified
        /// in the maximumValues parameter.  Dimensions are indicated by the index into each list.
        /// The minimum value of each dimension must be less than or equal to the maximum value
        /// of the dimension.  MinimumBoundingBox's can only be created for data types implementing
        /// the <see cref="IComparable"/> interface.
        /// </summary>
        /// <param name="minimumValues">The minimum value for each dimension.</param>
        /// <param name="maximumValues">The maximum value for each dimension.</param>
        /// <exception cref="InvalidDimensionException">Thrown when the number of dimensions is less than 1.</exception>
        /// <exception cref="IncompatibleDimensionsException">Thrown when the number of dimensions specified by the list of minimum values does not match the number of dimensions specified by the list of maximum values.</exception>
        /// <exception cref="InvalidRectangleException">Thrown when the minimum value of a dimension is greater than the maximum value of a dimension.</exception>
        public MinimumBoundingBox(List<CoordinateType> minimumValues, List<CoordinateType> maximumValues)
        {
            if (minimumValues.Count < 1)
                throw new InvalidDimensionException(minimumValues.Count);
            if (minimumValues.Count != maximumValues.Count)
                throw new IncompatibleDimensionsException(minimumValues.Count, maximumValues.Count);
            Dimension = minimumValues.Count;
            MinimumValues = minimumValues;
            MaximumValues = maximumValues;
            for (Int32 dimensionCounter = 0; dimensionCounter < Dimension; dimensionCounter++)
                if (minimumValues[dimensionCounter].CompareTo(maximumValues[dimensionCounter]) < 1)
                    throw new InvalidRectangleException<CoordinateType>(dimensionCounter, minimumValues[dimensionCounter], maximumValues[dimensionCounter]);
        }
        /// <summary>
        /// This constructor provides a means of reconstructing a MinimumBoundingBox
        /// object that has been saved using the <see cref="MinimumBoundingBox.GetBytes"/>
        /// method from the <see cref="ISavable"/> interface.
        /// </summary>
        /// <param name="byteData">The byte data saved from a previous MinimumBoundingBox object.</param>
        public unsafe MinimumBoundingBox(Byte[] byteData)
        {
            MinimumValues = new List<CoordinateType>();
            MaximumValues = new List<CoordinateType>();

            Type coordinateType = typeof(CoordinateType);
            Int32 coordinateSize = Marshal.SizeOf(coordinateType);
            if (!(byteData.Length > 0 && byteData.Length % (coordinateSize * 2) == 0))
                throw new InvalidMinimumBoundingBoxDataException();
            IntPtr coordinateBuffer = Marshal.AllocHGlobal(coordinateSize);
            for (Int32 i = 0; i < byteData.Length; i += coordinateSize)
            {
                Marshal.Copy(byteData, i, coordinateBuffer, coordinateSize);
                Marshal.PtrToStructure(coordinateBuffer, coordinateType);
                i += coordinateSize;
                Marshal.Copy(byteData, i, coordinateBuffer, coordinateSize);
                Marshal.PtrToStructure(coordinateBuffer, coordinateType);
            }
            Marshal.FreeHGlobal(coordinateBuffer);
            Dimension = MinimumValues.Count;
        }

        #endregion Public

        #endregion Constructors
        #region ISavable Methods

        /// <summary>
        /// This method generates an array of bytes that can be used to regenerate
        /// the object.  This is meant to be used for saving the object and later
        /// reconstruction.
        /// </summary>
        /// <remarks>
        /// The format of the byte array is pairs of values representing the
        /// minimum and then maximum for each dimension.  For instance
        /// {minX, maxX, minY, maxY} if the object described a cartesean plane.
        /// </remarks>
        /// <returns>An array of Bytes representing the Minimum and Maximum coordinates</returns>
        public virtual unsafe Byte[] GetBytes()
        {
            Int32 coordinateSize = Marshal.SizeOf(typeof(CoordinateType));
            Byte[] saveData = new Byte[GetSize()];
            IntPtr coordinateBuffer = Marshal.AllocHGlobal(coordinateSize);
            for (Int32 i = 0, saveDataLocation = 0; i < Dimension; i++, saveDataLocation += coordinateSize)
            {
                Marshal.StructureToPtr(MinimumValues[i], coordinateBuffer, false);
                Marshal.Copy(coordinateBuffer, saveData, saveDataLocation, coordinateSize);
                saveDataLocation += coordinateSize;
                Marshal.StructureToPtr(MaximumValues[i], coordinateBuffer, false);
                Marshal.Copy(coordinateBuffer, saveData, saveDataLocation, coordinateSize);
            }
            Marshal.FreeHGlobal(coordinateBuffer);
            return saveData;
            
        }
        /// <summary>
        /// This method returns the size of the object.  It calculates the size of the 
        /// generic type for this instance of the class and calculates the number of
        /// values of that type are needed for full reconstruction of the object.
        /// </summary>
        /// <returns>The size in bytes of the object.</returns>
        public virtual unsafe Int32 GetSize()
        {
            return Marshal.SizeOf(typeof(CoordinateType)) * Dimension * 2;
        }
        
        #endregion
    }
}