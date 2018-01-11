var map = function () { // article
    var summary = this.Content;

    if (summary) {
        // quick lowercase to normalize per your requirements
        summary = summary.toLowerCase().split(" ");

        for (var i = summary.length - 1; i >= 0; i--) {
            // might want to remove punctuation, etc. here

            if (summary[i]) { // make sure there's someting 
                var _id = { author: this.Author, word: summary[i] };
                var value = 1;


                emit(_id, value); // store a  for each word
            }
        }
    };

    var reduce = function (key, values) {
        return Array.sum(values);
    };

    db.articles.mapReduce(map, reduce, { limit: 1000, out: "word_count" });
    db.word_count.find().sort({ value: -1 }).limit(10);


    // word_count SCHEMA:
    //_id = {author: <string>, word: <string>}, value: <int>
    var mapper = function () {
        emit(this._id.author, { top: [{ word: this._id.word, total: this.value }] });
    };

    var reducer = function (key, values) {
        //var scores = [];
        var result = [];// { word: " ", scores: [] };
        values.forEach(
            function (value) { // value = { top [{ word, total }] }
                value.top.forEach(
                    function (score) { // score = {word, total}
                        result[result.length] = { word: score.word, total: score.total }; //  scores[scores.length] 
                    });
            });
        //var descending =
        result.sort((a, b) => Number(b.total) - Number(a.total));
        //scores.sort();
        //scores.reverse();
        return { top: result.slice(0, 10) };
    };

    db.word_count.mapReduce(mapper, reducer, "top_author_words");