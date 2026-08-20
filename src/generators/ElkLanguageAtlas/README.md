<table>
    <tr>
        <th valign="top">
            <h1>
                E<br/>
                L<br/>
                K<br/>
                L<br/>
                A<br/>
                N<br/>
                G<br/>
                ✦<br/>
                ✦
            </h1>
        </td>
        <td valign="top"><br/>

The ElkLang atlas tool takes in images in `input/`, premultiplies them, and compiles them into a single atlas with a respective .json definition file.

Images should be named in the format of `[NAME]-[OFFSET FROM LEFT]-[OFFSET FROM TOP]-[HEIGHT]`;
offsets and height should be provided as integers, use `m` to denote negatives.
Height is used to determine how much to offset the next character vertically when writing phrases.

Example:\
`BranchLeftA-m1-m2-14` would be drawn 1px to the left, 2px up, and have a height of 14.

> [!NOTE]
> - The horizontal offset is from the left edge of the area, the drawn area is 40px wide.
> - Height is applied regardless of the vertical offset, subtract the vertical offset from the height if applicable.
> - There is no max size for characters. 
        </td>
    </tr>
</table>

