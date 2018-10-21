using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CSVReader{

    private int rows, columns;
    private string[] header;
    private string[,] data;

    public int nRow { get { return rows; } }
    public int nCol { get { return nCol; } }

    public CSVReader(TextAsset text) {
        string[] lines = text.text.Split('\n');
        rows = lines.Length - 1;

        char separator = ';';
        header = lines[0].Split(separator);
        columns = header.Length;

        data = new string[rows, columns];
        for(int i = 0; i < rows; i++) {
            if(lines[i + 1].Trim().Length == 0) {
                if(i == rows - 1) {
                    rows--;
                    return;
                }
                else
                    Debug.LogError("Found blank line in CSV too soon at " + i);
            }
            string[] row = lines[i + 1].Split(separator);
            if(row.Length != columns)
                Debug.LogError("CSV file contains rows with inconsistent numbers " + 
                    "of entries; line " + (i + 1) + "with content: " + lines[i + 1]);
            for(int j = 0; j < columns; j++)
                data[i, j] = row[j];
        }
    }

    public void SortBy(string column) {
        SortBy(GetColumn(column));
    }

    public void SortBy(int column) {
        string[] sortColumn = new string[rows];
        int[] index = new int[rows];

        for(int i = 0; i < rows; i++) {
            sortColumn[i] = data[i, column];
            index[i] = i;
        }

        Array.Sort(index, (a, b) => Compare(sortColumn[a], sortColumn[b]));

        string[,] sortedData = new string[rows, columns];
        for(int i = 0; i < rows; i++) {
            for(int j = 0; j < columns; j++)
                sortedData[i, j] = data[index[i], j];
        }
        data = sortedData;
    }

    private int Compare(string a, string b) {
        int aInt, bInt;
        if(int.TryParse(a, out aInt) && int.TryParse(b, out bInt))
            return aInt.CompareTo(bInt);

        return string.Compare(a, b);
    }

    public string GetValue(int row, string column) {
        return GetValue(row, GetColumn(column));
    }

    public bool ColumnExists(string value) {
        try { GetColumn(value); }
        catch(Exception) {
            return false;
        }

        return true;
    }

    private int GetColumn(string value) {
        for(int i = 0; i < columns; i++) {
            if(header[i].ToLower().Contains(value.ToLower()))
                return i;
        }
        throw new System.Exception("Column name not found: " + value);
    }

    public int GetRow(string value, string column) {
        return GetRow(value, GetColumn(column));
    }

    public int GetRow(string value, int column) {
        for(int i = 0; i < rows; i++) {
            if(data[i, column] == value)
                return i;
        }
        throw new System.Exception("Value not found in column " + column + ": " + value);
    }

    public string GetValue(int row, int column) {
        return data[row, column];
    }
}
