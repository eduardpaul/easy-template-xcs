// Minimal docx inspection helpers used to check that every engine produces
// equivalent documents.

import JSZip from 'jszip';

export async function inspectDocx(bytes) {
    const zip = await JSZip.loadAsync(bytes);
    const xml = await zip.file('word/document.xml').async('string');
    const paragraphs = [...xml.matchAll(/<w:p[ >][\s\S]*?<\/w:p>/g)]
        .map(p => [...p[0].matchAll(/<w:t(?: [^>]*)?>([^<]*)<\/w:t>/g)].map(t => decode(t[1])).join(''));
    // image parts, wherever they live in the package (the Open XML SDK adds new
    // ones under /media, easy-template-x under /word/media)
    const media = Object.keys(zip.files).filter(f => /\.(png|jpe?g|gif|bmp|svg)$/i.test(f) && !zip.files[f].dir);
    return {
        text: paragraphs.join('\n'),
        paragraphs: paragraphs.length,
        tableRows: count(xml, /<w:tr[ >]/g),
        pageBreaks: count(xml, /<w:br w:type="page"\/>/g),
        images: count(xml, /<a:blip /g),
        media: media.length
    };
}

const count = (text, regex) => (text.match(regex) || []).length;

const decode = text => text
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, '&');
